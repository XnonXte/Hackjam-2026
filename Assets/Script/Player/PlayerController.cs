using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    // =========================================================================
    // EVENTS FOR ANIMATOR & AUDIO
    // =========================================================================
    public event Action OnDeath;
    public event Action OnRevive;
    public event Action OnDash;
    public event Action OnJump; 
    public event Action OnLedgeJump; // <-- Added event for ledge jumping / climbing

    // =========================================================================
    // PUBLIC STATE
    // =========================================================================
    public bool IsGrounded => isGrounded;
    public bool IsDead { get; private set; } = false;
    public bool IsDashing => isDashing;
    public float FacingDir => facingDir;
    public Vector2 Velocity => rb.linearVelocity;
    public bool IsOnWall => isOnWall;
    
    [HideInInspector] public bool isInputLocked = false;

    // =========================================================================
    // ABILITIES
    // =========================================================================
    [Header("Abilities")]
    public bool canJump = true;
    public bool canDash = true;
    public bool canWallClimb = true;

    // =========================================================================
    // INPUT ACTION REFERENCES
    // =========================================================================
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference dashAction;
    [SerializeField] private InputActionReference grabAction;

    [Header("Check Points")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float checkRadius = 0.15f;
    [SerializeField] private float ledgeCheckOffset = 0.15f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask unclimbableLayer;

    [Header("Movement")]
    [SerializeField] private float maxSpeed = 9f;
    [SerializeField] private float groundAcceleration = 80f;
    [SerializeField] private float groundDeceleration = 80f;
    [SerializeField] private float airAccelerationMultiplier = 0.65f;
    [SerializeField] private float airDecelerationMultiplier = 0.65f;

    [Header("Jumping")]
    [SerializeField] private float jumpVelocity = 18f;
    [SerializeField] private float gravityScale = 5f;
    [SerializeField] private float fallGravityScale = 7f;
    [SerializeField] private float apexGravityScale = 2.5f;
    [SerializeField] private float apexThreshold = 3f;

    [Header("Coyote Time & Buffers")]
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float ledgePopHeight = 0.3f;

    [Header("Dashing")]
    [SerializeField] private float dashSpeed = 22f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private bool dashZerosVerticalVelocity = true;
    [SerializeField] private float dashEndSpeedCap = 13f;
    [SerializeField] private float dashHitStop = 0.04f;
    
    [Header("Wall Climbing")]
    [SerializeField] private float wallClimbSpeed = 5f;
    [SerializeField] private float wallSlideSpeed = 4f;
    [SerializeField] private float wallJumpHorizontalSpeed = 12f;
    [SerializeField] private float wallJumpVerticalSpeed = 16f;
    [SerializeField] private float wallCoyoteTime = 0.1f;
    [SerializeField] private Vector2 ledgeClimbPush = new Vector2(6f, 10f);

    private Rigidbody2D rb;
    private Collider2D col;

    private bool isGrounded, wasGrounded;
    private bool isTouchingWallLeft, isTouchingWallRight, isOnWall;
    private float wallDir, wallCoyoteTimer;
    private float coyoteTimer, jumpBufferTimer;
    private bool isJumping, jumpHeld;
    private bool hasDash = true, isDashing;
    private float facingDir = 1f;

    private float moveInput, verticalInput;
    private bool grabHeld;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        PhysicsMaterial2D noFriction = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
        rb.sharedMaterial = noFriction;
        col.sharedMaterial = noFriction;

        rb.gravityScale = gravityScale;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();
        dashAction.action.Enable();
        grabAction.action.Enable();

        jumpAction.action.started += OnJumpStarted;
        jumpAction.action.canceled += OnJumpCanceled;
        dashAction.action.started += OnDashStarted;
    }

    private void OnDisable()
    {
        jumpAction.action.started -= OnJumpStarted;
        jumpAction.action.canceled -= OnJumpCanceled;
        dashAction.action.started -= OnDashStarted;

        moveAction.action.Disable();
        jumpAction.action.Disable();
        dashAction.action.Disable();
        grabAction.action.Disable();
    }

    private void Update()
    {
        if (IsDead) return;

        if (isInputLocked)
        {
            moveInput = 0f;
            verticalInput = 0f;
            grabHeld = false;
            return;
        }

        Vector2 raw = moveAction.action.ReadValue<Vector2>();
        moveInput = raw.x;
        verticalInput = raw.y;
        grabHeld = canWallClimb && grabAction.action.IsPressed();

        if (Mathf.Abs(moveInput) > 0.1f) SetFacing(Mathf.Sign(moveInput));

        TickTimers();
    }

    private void FixedUpdate()
    {
        if (IsDead) return;

        wasGrounded = isGrounded;
        isGrounded = CheckGround();
        isTouchingWallLeft = CheckWallLeft();
        isTouchingWallRight = CheckWallRight();

        EvaluateWallContact();

        if (isGrounded && !wasGrounded)
        {
            isJumping = false;
            if (!hasDash) hasDash = true;
        }

        if (!isGrounded && wasGrounded && !isDashing)
            coyoteTimer = coyoteTime;

        if (isDashing)
        {
            rb.gravityScale = 0f;
            return;
        }

        if (isOnWall)
        {
            HandleWallClimbing();
            TryConsumeWallJumpBuffer();
        }
        else
        {
            HandleHorizontalMovement();
            TryConsumeJumpBuffer();
            ApplyGravityModifiers();
            TryLedgePop();
        }
    }

    public void ApplySacrifices(List<string> sacrificedAbilities)
    {
        canJump = !sacrificedAbilities.Contains("Jump");
        canDash = !sacrificedAbilities.Contains("Dash");
        canWallClimb = !sacrificedAbilities.Contains("WallClimb");
    }

    public void Die()
    {
        if (IsDead) return;
        IsDead = true;
        Time.timeScale = 1f; 

        StopAllCoroutines();
        
        isDashing = false;
        isOnWall = false;
        isJumping = false;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;

        OnDeath?.Invoke();
    }

    public void Revive()
    {
        IsDead = false;
        
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = gravityScale;
        
        hasDash = true;
        isDashing = false;
        
        OnRevive?.Invoke();
    }

    private void OnJumpStarted(InputAction.CallbackContext ctx)
    {
        if (IsDead || !canJump || isInputLocked) return;
        jumpHeld = true;
        jumpBufferTimer = jumpBufferTime;
    }

    private void OnJumpCanceled(InputAction.CallbackContext ctx)
    {
        if (IsDead || !canJump || isInputLocked) return;
        jumpHeld = false;

        if (isJumping && !isOnWall && rb.linearVelocity.y > 0f)
        {
            isJumping = false;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }
    }

    private void OnDashStarted(InputAction.CallbackContext ctx)
    {
        if (IsDead || !hasDash || isDashing || !canDash || isInputLocked) return;
        float dir = Mathf.Abs(moveInput) > 0.1f ? Mathf.Sign(moveInput) : facingDir;
        StartCoroutine(DashRoutine(dir));
    }

    private IEnumerator DashRoutine(float dir)
    {
        hasDash = false;
        isDashing = true;
        SetFacing(dir);
        OnDash?.Invoke(); 

        if (dashHitStop > 0f)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(dashHitStop);
            Time.timeScale = 1f;
        }

        rb.gravityScale = 0f;
        rb.linearVelocity = dashZerosVerticalVelocity
            ? new Vector2(dir * dashSpeed, 0f)
            : new Vector2(dir * dashSpeed, rb.linearVelocity.y);

        coyoteTimer = 0f;
        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            isGrounded = CheckGround();
            if (isGrounded)
            {
                hasDash = true;
            }

            rb.linearVelocity = new Vector2(dir * dashSpeed, dashZerosVerticalVelocity ? 0f : rb.linearVelocity.y);
            elapsed += Time.deltaTime;
            yield return null;
        }

        float vx = Mathf.Clamp(rb.linearVelocity.x, -dashEndSpeedCap, dashEndSpeedCap);
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
        rb.gravityScale = gravityScale;
        isDashing = false;

        if (CheckGround())
        {
            hasDash = true;
        }
    }

    private void TickTimers()
    {
        if (coyoteTimer > 0f) coyoteTimer -= Time.deltaTime;
        if (jumpBufferTimer > 0f) jumpBufferTimer -= Time.deltaTime;
        if (wallCoyoteTimer > 0f) wallCoyoteTimer -= Time.deltaTime;
    }

    private bool CheckGround() => groundCheck != null && Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer | unclimbableLayer);
    private bool CheckWallLeft() => Physics2D.OverlapCircle(new Vector2(col.bounds.min.x, col.bounds.center.y), checkRadius, groundLayer);
    private bool CheckWallRight() => Physics2D.OverlapCircle(new Vector2(col.bounds.max.x, col.bounds.center.y), checkRadius, groundLayer);
    private bool CheckLedgeLeft() => Physics2D.OverlapCircle(new Vector2(col.bounds.min.x, col.bounds.max.y - ledgeCheckOffset), checkRadius, groundLayer);
    private bool CheckLedgeRight() => Physics2D.OverlapCircle(new Vector2(col.bounds.max.x, col.bounds.max.y - ledgeCheckOffset), checkRadius, groundLayer);

    private void EvaluateWallContact()
    {
        bool touchingAnyWall = isTouchingWallLeft || isTouchingWallRight;
        bool wasOnWall = isOnWall;
        isOnWall = grabHeld && touchingAnyWall;

        if (isOnWall)
        {
            wallDir = isTouchingWallRight ? 1f : -1f;

            if (!wasOnWall)
            {
                SetFacing(wallDir);
                SnapToWall(wallDir);
            }
        }
        else if (wasOnWall)
        {
            wallCoyoteTimer = wallCoyoteTime;
        }
    }

    private void SnapToWall(float dir)
    {
        Bounds b = col.bounds;
        float castDistance = checkRadius + 0.05f; 

        RaycastHit2D hit = Physics2D.Raycast(
            new Vector2(dir > 0f ? b.max.x : b.min.x, b.center.y),
            Vector2.right * dir,
            castDistance,
            groundLayer);

        if (hit.collider != null)
        {
            float halfWidth = b.extents.x;
            float targetCenterX = hit.point.x - dir * halfWidth;
            float delta = targetCenterX - b.center.x; 
            transform.position += new Vector3(delta, 0f, 0f);
        }
    }

    private void HandleWallClimbing()
    {
        rb.gravityScale = 0f;
        float vy = 0f;
        if (verticalInput > 0.3f) vy = wallClimbSpeed;
        else if (verticalInput < -0.3f && !isGrounded) vy = -wallSlideSpeed;

        rb.linearVelocity = new Vector2(0f, vy);

        if (verticalInput > 0.3f)
        {
            if (wallDir == -1f && isTouchingWallLeft && !CheckLedgeLeft()) PushOntoLedge(-1f);
            else if (wallDir == 1f && isTouchingWallRight && !CheckLedgeRight()) PushOntoLedge(1f);
        }
    }

    private void PushOntoLedge(float dir)
    {
        isOnWall = false;
        transform.position += new Vector3(dir * 0.15f, 0.2f, 0f);
        rb.linearVelocity = new Vector2(dir * ledgeClimbPush.x, ledgeClimbPush.y);
        
        OnLedgeJump?.Invoke(); // <-- Fire ledge jump event here
    }

    private void TryConsumeWallJumpBuffer()
    {
        bool canWallJump = isOnWall || wallCoyoteTimer > 0f;
        if (jumpBufferTimer <= 0f || !canWallJump) return;

        bool pushingIntoWall = Mathf.Abs(moveInput) > 0.1f && Mathf.Sign(moveInput) == wallDir;

        if (pushingIntoWall)
        {
            rb.linearVelocity = new Vector2(0f, wallJumpVerticalSpeed);
        }
        else
        {
            float pushDir = -wallDir;
            rb.linearVelocity = new Vector2(pushDir * wallJumpHorizontalSpeed, wallJumpVerticalSpeed);
            SetFacing(pushDir);
            hasDash = true;
        }

        jumpBufferTimer = 0f;
        wallCoyoteTimer = 0f;
        isOnWall = false;
        isJumping = true;
        rb.gravityScale = gravityScale;
        
        OnJump?.Invoke();
    }

    private void SetFacing(float dir)
    {
        if (Mathf.Approximately(dir, facingDir)) return;
        facingDir = dir;
    }

    private void HandleHorizontalMovement()
    {
        float vx = rb.linearVelocity.x;
        float accel = isGrounded ? groundAcceleration : groundAcceleration * airAccelerationMultiplier;
        float decel = isGrounded ? groundDeceleration : groundDeceleration * airDecelerationMultiplier;

        if (Mathf.Abs(moveInput) > 0.01f)
            vx = Mathf.MoveTowards(vx, moveInput * maxSpeed, accel * Time.fixedDeltaTime);
        else
            vx = Mathf.MoveTowards(vx, 0f, decel * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
    }

    private void TryConsumeJumpBuffer()
    {
        if (jumpBufferTimer > 0f && (isGrounded || coyoteTimer > 0f))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVelocity);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            isJumping = true;
            
            OnJump?.Invoke();
        }
    }

    private void ApplyGravityModifiers()
    {
        float vy = rb.linearVelocity.y;
        bool atApex = isJumping && jumpHeld && vy > 0f && Mathf.Abs(vy) < apexThreshold;

        if (atApex) rb.gravityScale = apexGravityScale;
        else if (vy < 0f) rb.gravityScale = fallGravityScale;
        else rb.gravityScale = gravityScale;
    }

    private void TryLedgePop()
    {
        if (isGrounded || rb.linearVelocity.y <= 0f || Mathf.Abs(rb.linearVelocity.x) < 0.1f) return;

        Bounds b = col.bounds;
        float dir = Mathf.Sign(rb.linearVelocity.x);
        float side = dir > 0f ? b.max.x : b.min.x;

        RaycastHit2D hit = Physics2D.Raycast(
            new Vector2(side, b.min.y + 0.01f) + Vector2.up * ledgePopHeight,
            Vector2.down, ledgePopHeight, groundLayer | unclimbableLayer);

        if (hit.collider != null)
        {
            float pop = hit.point.y - b.min.y;
            if (pop > 0f && pop <= ledgePopHeight)
            {
                transform.position += new Vector3(0f, pop, 0f);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Collider2D previewCol = col != null ? col : GetComponent<Collider2D>();
        if (previewCol == null) return;

        Bounds b = previewCol.bounds;

        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(b.center, b.size);

        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(new Vector2(b.min.x, b.center.y), checkRadius);
        Gizmos.DrawWireSphere(new Vector2(b.max.x, b.center.y), checkRadius);

        Gizmos.color = Color.yellow;
        Vector2 leftLedgePos = new Vector2(b.min.x, b.max.y - ledgeCheckOffset);
        Vector2 rightLedgePos = new Vector2(b.max.x, b.max.y - ledgeCheckOffset);

        Gizmos.DrawWireSphere(leftLedgePos, checkRadius);
        Gizmos.DrawWireSphere(rightLedgePos, checkRadius);
    }
}