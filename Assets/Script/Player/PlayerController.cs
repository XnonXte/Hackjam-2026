using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Basic Movement")]
    public float moveSpeed = 8f;
    public float groundAcceleration = 80f;
    public float groundDeceleration = 80f;
    public float airAcceleration = 60f;
    public float airDeceleration = 60f;

    [Header("Gravity")]
    public float baseGravityScale = 4f;
    public float fallGravityMultiplier = 1.5f;
    public float jumpGravityMultiplier = 1f;

    [Header("Jump")]
    public float jumpForce = 14f;

    [Range(0f, 1f)]
    public float jumpCutMultiplier = 0.45f;

    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;

    [Header("Dash")]
    public float dashSpeed = 24f;
    public float dashDuration = 0.15f;

    [Header("Wall Mechanics")]
    public float wallClimbSpeed = 4f;
    public float wallSlideSpeed = 2f;
    public Vector2 wallJumpForce = new Vector2(12f, 16f);

    [Header("Ledge Climb")]
    public float ledgeForwardDistance = 0.5f;
    public float ledgeClimbHeight = 1.2f;
    public float ledgeDetectionDistance = 0.5f;
    public float ledgeClimbDuration = 0.12f;

    [Header("Collision Detection")]
    public Transform groundCheck;
    public Transform leftWallCheck;
    public Transform rightWallCheck;

    public float checkRadius = 0.12f;
    public LayerMask whatIsGround;

    [Header("Input Actions")]
    public InputActionReference moveAction;
    public InputActionReference jumpAction;
    public InputActionReference dashAction;
    public InputActionReference grabAction;

    private Rigidbody2D rb;

    private Vector2 moveInput;

    private float coyoteTimer;
    private float jumpBufferTimer;

    private int facingDirection = 1;
    private int wallDirection;

    private bool isGrounded;
    private bool isTouchingLeftWall;
    private bool isTouchingRightWall;
    private bool isTouchingWall;

    private bool isDashing;
    private bool isGrabbing;
    private bool isWallSliding;
    private bool isClimbingLedge;

    private bool canDash = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = baseGravityScale;
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();
        dashAction.action.Enable();
        grabAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        jumpAction.action.Disable();
        dashAction.action.Disable();
        grabAction.action.Disable();
    }

    private void Update()
    {
        if (isDashing || isClimbingLedge)
            return;

        moveInput = moveAction.action.ReadValue<Vector2>();

        UpdateEnvironment();
        UpdateTimers();

        // Handle jump FIRST.
        HandleJump();

        // Wall mechanics happen after jump.
        HandleWallMechanics();

        HandleDash();
        HandleFacingDirection();

        // Only attempt ledge climbing while grabbing.
        if (isGrabbing)
        {
            TryClimbLedge();
        }
    }

    private void FixedUpdate()
    {
        if (isDashing || isClimbingLedge)
            return;

        if (!isGrabbing)
        {
            ApplyMovement();
            ApplyGravity();
        }
    }

    // =========================
    // ENVIRONMENT
    // =========================

    private void UpdateEnvironment()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            checkRadius,
            whatIsGround
        );

        isTouchingLeftWall = Physics2D.OverlapCircle(
            leftWallCheck.position,
            checkRadius,
            whatIsGround
        );

        isTouchingRightWall = Physics2D.OverlapCircle(
            rightWallCheck.position,
            checkRadius,
            whatIsGround
        );

        isTouchingWall =
            isTouchingLeftWall ||
            isTouchingRightWall;

        if (isTouchingLeftWall)
        {
            wallDirection = -1;
        }
        else if (isTouchingRightWall)
        {
            wallDirection = 1;
        }
        else
        {
            wallDirection = 0;
        }

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;

            // Dash can ONLY be restored on the ground.
            canDash = true;
        }
    }

    private void UpdateTimers()
    {
        if (!isGrounded)
        {
            coyoteTimer -= Time.deltaTime;
        }

        if (jumpBufferTimer > 0f)
        {
            jumpBufferTimer -= Time.deltaTime;
        }
    }

    // =========================
    // MOVEMENT
    // =========================

    private void ApplyMovement()
    {
        float targetSpeed = moveInput.x * moveSpeed;

        bool hasInput = Mathf.Abs(moveInput.x) > 0.01f;

        float acceleration;

        if (isGrounded)
        {
            acceleration = hasInput
                ? groundAcceleration
                : groundDeceleration;
        }
        else
        {
            acceleration = hasInput
                ? airAcceleration
                : airDeceleration;
        }

        float velocityX = Mathf.MoveTowards(
            rb.linearVelocity.x,
            targetSpeed,
            acceleration * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector2(
            velocityX,
            rb.linearVelocity.y
        );
    }

    // =========================
    // GRAVITY
    // =========================

    private void ApplyGravity()
    {
        if (isGrounded)
        {
            rb.gravityScale = baseGravityScale;
            return;
        }

        if (rb.linearVelocity.y < 0f)
        {
            rb.gravityScale =
                baseGravityScale * fallGravityMultiplier;
        }
        else
        {
            rb.gravityScale =
                baseGravityScale * jumpGravityMultiplier;
        }
    }

    // =========================
    // FACING
    // =========================

    private void HandleFacingDirection()
    {
        if (Mathf.Abs(moveInput.x) < 0.01f)
            return;

        int direction = moveInput.x > 0f ? 1 : -1;

        if (direction != facingDirection)
        {
            Flip();
        }
    }

    // =========================
    // JUMP
    // =========================

    private void HandleJump()
    {
        if (jumpAction.action.WasPressedThisFrame())
        {
            jumpBufferTimer = jumpBufferTime;
        }

        if (jumpBufferTimer > 0f)
        {
            // Ground jump.
            if (coyoteTimer > 0f)
            {
                PerformGroundJump();
                return;
            }

            // Wall jump.
            // Grab state does NOT matter.
            if (isTouchingWall)
            {
                PerformWallJump();
                return;
            }
        }

        // Variable jump height.
        if (
            jumpAction.action.WasReleasedThisFrame() &&
            rb.linearVelocity.y > 0f
        )
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                rb.linearVelocity.y * jumpCutMultiplier
            );
        }
    }

    private void PerformGroundJump()
    {
        isGrabbing = false;
        isWallSliding = false;

        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            jumpForce
        );

        jumpBufferTimer = 0f;
        coyoteTimer = 0f;

        rb.gravityScale =
            baseGravityScale * jumpGravityMultiplier;
    }

    private void PerformWallJump()
    {
        isGrabbing = false;
        isWallSliding = false;

        // Jump away from the wall.
        rb.linearVelocity = new Vector2(
            -wallDirection * wallJumpForce.x,
            wallJumpForce.y
        );

        jumpBufferTimer = 0f;
        coyoteTimer = 0f;

        rb.gravityScale =
            baseGravityScale * jumpGravityMultiplier;

        // Face away from the wall.
        if (facingDirection != -wallDirection)
        {
            Flip();
        }
    }

    // =========================
    // WALL MECHANICS
    // =========================

    private void HandleWallMechanics()
    {
        bool grabHeld = grabAction.action.IsPressed();

        if (isTouchingWall && grabHeld)
        {
            isGrabbing = true;
            isWallSliding = false;

            rb.gravityScale = 0f;

            rb.linearVelocity = new Vector2(
                0f,
                moveInput.y * wallClimbSpeed
            );
        }
        else if (
            isTouchingWall &&
            !isGrounded &&
            rb.linearVelocity.y < 0f
        )
        {
            isGrabbing = false;
            isWallSliding = true;

            rb.gravityScale = baseGravityScale;

            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                Mathf.Max(
                    rb.linearVelocity.y,
                    -wallSlideSpeed
                )
            );
        }
        else
        {
            isGrabbing = false;
            isWallSliding = false;
        }
    }

    // =========================
    // LEDGE CLIMB
    // =========================

    private void TryClimbLedge()
    {
        if (!isTouchingWall)
            return;

        if (wallDirection == 0)
            return;

        /*
         * Start from the wall.
         *
         * For a left wall:
         *     wallDirection = -1
         *
         * For a right wall:
         *     wallDirection = 1
         *
         * Multiplying by wallDirection makes all
         * horizontal calculations symmetrical.
         */

        Vector2 wallPosition;

        if (wallDirection == -1)
        {
            wallPosition = leftWallCheck.position;
        }
        else
        {
            wallPosition = rightWallCheck.position;
        }

        // Check upward from the wall.
        Vector2 topCheckOrigin =
            wallPosition +
            Vector2.up * ledgeClimbHeight;

        bool blockedAbove = Physics2D.OverlapCircle(
            topCheckOrigin,
            checkRadius,
            whatIsGround
        );

        if (blockedAbove)
            return;

        // Move toward the top of the ledge.
        Vector2 ledgeCheckOrigin =
            topCheckOrigin +
            Vector2.right *
            wallDirection *
            ledgeForwardDistance;

        RaycastHit2D ledgeHit = Physics2D.Raycast(
            ledgeCheckOrigin,
            Vector2.down,
            ledgeClimbHeight,
            whatIsGround
        );

        if (ledgeHit.collider == null)
            return;

        StartCoroutine(
            ClimbLedgeRoutine(ledgeHit.point)
        );
    }

    private IEnumerator ClimbLedgeRoutine(
        Vector2 ledgePoint
    )
    {
        isClimbingLedge = true;

        isGrabbing = false;
        isWallSliding = false;

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        Vector3 startPosition = transform.position;

        // Move slightly toward the center of the platform.
        Vector3 targetPosition = new Vector3(
            ledgePoint.x +
            wallDirection * ledgeForwardDistance,

            ledgePoint.y + 0.5f,

            transform.position.z
        );

        float elapsed = 0f;

        while (elapsed < ledgeClimbDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / ledgeClimbDuration
            );

            // Smooth interpolation.
            t = t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                t
            );

            yield return null;
        }

        transform.position = targetPosition;

        rb.gravityScale = baseGravityScale;

        isClimbingLedge = false;
    }

    // =========================
    // DASH
    // =========================

    private void HandleDash()
    {
        if (
            dashAction.action.WasPressedThisFrame() &&
            canDash
        )
        {
            StartCoroutine(DashRoutine());
        }
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        canDash = false;

        rb.gravityScale = 0f;

        rb.linearVelocity = new Vector2(
            facingDirection * dashSpeed,
            0f
        );

        yield return new WaitForSeconds(
            dashDuration
        );

        rb.gravityScale = baseGravityScale;

        isDashing = false;
    }

    // =========================
    // CHARACTER
    // =========================

    private void Flip()
    {
        facingDirection *= -1;

        Vector3 scale = transform.localScale;
        scale.x *= -1f;

        transform.localScale = scale;
    }

    // =========================
    // DEBUG
    // =========================

    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;

            Gizmos.DrawWireSphere(
                groundCheck.position,
                checkRadius
            );
        }

        if (leftWallCheck != null)
        {
            Gizmos.color = Color.blue;

            Gizmos.DrawWireSphere(
                leftWallCheck.position,
                checkRadius
            );
        }

        if (rightWallCheck != null)
        {
            Gizmos.color = Color.blue;

            Gizmos.DrawWireSphere(
                rightWallCheck.position,
                checkRadius
            );
        }
    }
}