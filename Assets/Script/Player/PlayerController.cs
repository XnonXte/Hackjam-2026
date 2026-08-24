using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Celeste-inspired PlayerController — Unity New Input System edition.
///
/// Drag actions from your .inputactions asset into the inspector slots:
///   · Move  → Value / Vector2  (WASD, left stick, d-pad)
///   · Jump  → Button           (Space, South Button)
///   · Dash  → Button           (Shift, East Button, R2…)
///   · Grab  → Button (hold)    (Z, Left Bumper…)
///
/// Drag empty child GameObjects into the check point slots:
///   · groundCheck      — place at the bottom centre of the collider
///   · wallCheckLeft    — place at the left side, vertically centred or near bottom
///   · wallCheckRight   — place at the right side, vertically centred or near bottom
///   · ledgeCheckLeft   — place at the TOP left side of the collider
///   · ledgeCheckRight  — place at the TOP right side of the collider
///
/// Systems implemented:
///   Movement      — acceleration / deceleration, air control, sprite flip
///   Jumping       — variable height, coyote time, jump buffering,
///                   corner correction, ledge popping, apex hang-time
///   Dashing       — left/right burst, momentum conservation,
///                   one dash per airtime, ground recharge
///   Wall Climbing — grab (stick), slide down, climb up, ledge vaulting,
///                   wall-jump with wide detection window and coyote time
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    // =========================================================================
    // INPUT ACTION REFERENCES
    // =========================================================================

    [Header("Input Actions")]
    [Tooltip("Value/Vector2 — horizontal axis (WASD, stick, d-pad…)")]
    [SerializeField] private InputActionReference moveAction;

    [Tooltip("Button — jump (Space, South Button…)")]
    [SerializeField] private InputActionReference jumpAction;

    [Tooltip("Button — dash (Shift, East Button…)")]
    [SerializeField] private InputActionReference dashAction;

    [Tooltip("Button (hold) — grab wall (Z, Left Bumper…)")]
    [SerializeField] private InputActionReference grabAction;

    // =========================================================================
    // CHECK POINTS  (assign empty child GameObjects in the inspector)
    // =========================================================================

    [Header("Check Points")]
    [Tooltip("Empty child GO at the bottom-centre of the collider. " +
             "Used for ground overlap detection.")]
    [SerializeField] private Transform groundCheck;

    [Tooltip("Empty child GO at the left side of the collider. " +
             "Used for left-wall overlap detection.")]
    [SerializeField] private Transform wallCheckLeft;

    [Tooltip("Empty child GO at the right side of the collider. " +
             "Used for right-wall overlap detection.")]
    [SerializeField] private Transform wallCheckRight;

    [Tooltip("Empty child GO at the TOP-left of the collider. " +
             "Used to detect when the top of the player clears the wall.")]
    [SerializeField] private Transform ledgeCheckLeft;

    [Tooltip("Empty child GO at the TOP-right of the collider. " +
             "Used to detect when the top of the player clears the wall.")]
    [SerializeField] private Transform ledgeCheckRight;

    [Tooltip("Radius of the OverlapCircle used for each check point.")]
    [SerializeField] private float checkRadius = 0.15f;

    // =========================================================================
    // INSPECTOR TUNABLES
    // =========================================================================

    [Header("Movement")]
    [Tooltip("Top horizontal speed (units/sec)")]
    [SerializeField] private float maxSpeed = 9f;

    [Tooltip("Ground acceleration (units/sec²)")]
    [SerializeField] private float groundAcceleration = 80f;

    [Tooltip("Ground deceleration (units/sec²)")]
    [SerializeField] private float groundDeceleration = 80f;

    [Tooltip("Air acceleration multiplier")]
    [SerializeField] private float airAccelerationMultiplier = 0.65f;

    [Tooltip("Air deceleration multiplier")]
    [SerializeField] private float airDecelerationMultiplier = 0.65f;

    // -------------------------------------------------------------------------
    [Header("Sprite")]
    [Tooltip("SpriteRenderer to flip when the player changes direction. " +
             "Leave empty to skip flipping.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    // -------------------------------------------------------------------------
    [Header("Jumping")]
    [Tooltip("Initial vertical velocity on jump")]
    [SerializeField] private float jumpVelocity = 18f;

    [Tooltip("Normal gravity scale (rising)")]
    [SerializeField] private float gravityScale = 5f;

    [Tooltip("Gravity scale while falling")]
    [SerializeField] private float fallGravityScale = 7f;

    [Tooltip("Gravity scale at apex while jump held (hang-time)")]
    [SerializeField] private float apexGravityScale = 2.5f;

    [Tooltip("Speed window (units/sec) treated as the jump apex")]
    [SerializeField] private float apexThreshold = 3f;

    // -------------------------------------------------------------------------
    [Header("Coyote Time")]
    [Tooltip("Seconds after leaving a ledge that a jump is still allowed")]
    [SerializeField] private float coyoteTime = 0.12f;

    [Header("Jump Buffering")]
    [Tooltip("Seconds an early jump press is remembered")]
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Corner Correction")]
    [Tooltip("Max horizontal nudge to slip past a ceiling corner")]
    [SerializeField] private float cornerCorrectionWidth = 0.35f;

    [Tooltip("Rays per side for corner correction")]
    [SerializeField] private int cornerCorrectionRays = 3;

    [Header("Ledge Popping")]
    [Tooltip("Max height the player is snapped up onto a platform edge")]
    [SerializeField] private float ledgePopHeight = 0.3f;

    [Header("Ground / Wall Detection")]
    [Tooltip("Layer(s) treated as solid ground and walls")]
    [SerializeField] private LayerMask groundLayer;

    // -------------------------------------------------------------------------
    [Header("Dashing")]
    [Tooltip("Horizontal speed at dash start (units/sec)")]
    [SerializeField] private float dashSpeed = 22f;

    [Tooltip("Dash burst duration (seconds)")]
    [SerializeField] private float dashDuration = 0.15f;

    [Tooltip("Zero vertical velocity when dash starts")]
    [SerializeField] private bool dashZerosVerticalVelocity = true;

    [Tooltip("Horizontal speed cap when the dash ends (momentum handoff)")]
    [SerializeField] private float dashEndSpeedCap = 13f;

    [Tooltip("Freeze-frame at dash start (seconds). 0 = disabled.")]
    [SerializeField] private float dashHitStop = 0.04f;

    // -------------------------------------------------------------------------
    [Header("Wall Climbing")]
    [Tooltip("Speed the player climbs up while holding Up and grabbing (units/sec)")]
    [SerializeField] private float wallClimbSpeed = 5f;

    [Tooltip("Speed the player climbs down while holding Down and grabbing (units/sec)")]
    [SerializeField] private float wallSlideSpeed = 4f;

    [Tooltip("Horizontal speed when jumping away from a wall")]
    [SerializeField] private float wallJumpHorizontalSpeed = 12f;

    [Tooltip("Vertical speed on any wall-jump")]
    [SerializeField] private float wallJumpVerticalSpeed = 16f;

    [Tooltip("Seconds after leaving a wall that a wall-jump is still allowed " +
             "(wall equivalent of coyote time)")]
    [SerializeField] private float wallCoyoteTime = 0.1f;

    [Header("Ledge Vaulting")]
    [Tooltip("Velocity boost applied to push the player onto the ledge when reaching the top")]
    [SerializeField] private Vector2 ledgeClimbPush = new Vector2(6f, 10f);

    // =========================================================================
    // PRIVATE STATE
    // =========================================================================

    private Rigidbody2D   rb;
    private Collider2D    col;

    // ── Ground ────────────────────────────────────────────────────────────────
    private bool isGrounded;
    private bool wasGrounded;

    // ── Wall ──────────────────────────────────────────────────────────────────
    private bool  isTouchingWallLeft;
    private bool  isTouchingWallRight;
    private bool  isOnWall;      // grabbing AND touching a wall
    private float wallDir;       // +1 = right wall, -1 = left wall
    private float wallCoyoteTimer;

    // ── Timers ────────────────────────────────────────────────────────────────
    private float coyoteTimer;
    private float jumpBufferTimer;

    // ── Jump ──────────────────────────────────────────────────────────────────
    private bool isJumping;
    private bool jumpHeld;

    // ── Dash ──────────────────────────────────────────────────────────────────
    private bool  hasDash = true;
    private bool  isDashing;
    private float dashDirection;

    // ── Facing ────────────────────────────────────────────────────────────────
    private float facingDir = 1f;

    // ── Input (cached Update → consumed FixedUpdate) ──────────────────────────
    private float moveInput;
    private float verticalInput;
    private bool  grabHeld;

    // =========================================================================
    // PUBLIC READ-ONLY  (hook into Animator)
    // =========================================================================

    public bool  IsGrounded           => isGrounded;
    public bool  IsTouchingWallLeft   => isTouchingWallLeft;
    public bool  IsTouchingWallRight  => isTouchingWallRight;
    public bool  IsOnWall             => isOnWall;
    public bool  IsDashing            => isDashing;
    public float FacingDir            => facingDir;

    // =========================================================================
    // UNITY LIFECYCLE
    // =========================================================================

    private void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // 1. FIX FRICTION ISSUES: 
        // Eliminates snagging on floor tiles and sticking to walls without grabbing.
        PhysicsMaterial2D noFriction = new PhysicsMaterial2D("NoFriction");
        noFriction.friction = 0f;
        noFriction.bounciness = 0f;
        rb.sharedMaterial = noFriction;
        col.sharedMaterial = noFriction;

        rb.gravityScale   = gravityScale;
        rb.freezeRotation = true;
        rb.interpolation  = RigidbodyInterpolation2D.Interpolate;
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();
        dashAction.action.Enable();
        grabAction.action.Enable();

        jumpAction.action.started  += OnJumpStarted;
        jumpAction.action.canceled += OnJumpCanceled;
        dashAction.action.started  += OnDashStarted;
    }

    private void OnDisable()
    {
        jumpAction.action.started  -= OnJumpStarted;
        jumpAction.action.canceled -= OnJumpCanceled;
        dashAction.action.started  -= OnDashStarted;

        moveAction.action.Disable();
        jumpAction.action.Disable();
        dashAction.action.Disable();
        grabAction.action.Disable();
    }

    // =========================================================================
    // INPUT CALLBACKS
    // =========================================================================

    private void OnJumpStarted(InputAction.CallbackContext ctx)
    {
        jumpHeld        = true;
        jumpBufferTimer = jumpBufferTime;
    }

    private void OnJumpCanceled(InputAction.CallbackContext ctx)
    {
        jumpHeld = false;

        // Variable-height cut — only for normal jumps, not wall-jumps
        if (isJumping && !isOnWall && rb.linearVelocity.y > 0f)
        {
            isJumping = false;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }
    }

    private void OnDashStarted(InputAction.CallbackContext ctx)
    {
        if (!hasDash || isDashing) return;
        float dir = Mathf.Abs(moveInput) > 0.1f ? Mathf.Sign(moveInput) : facingDir;
        StartCoroutine(DashRoutine(dir));
    }

    // =========================================================================
    // UPDATE — input polling & timers
    // =========================================================================

    private void Update()
    {
        Vector2 raw   = moveAction.action.ReadValue<Vector2>();
        moveInput     = raw.x;
        verticalInput = raw.y;
        grabHeld      = grabAction.action.IsPressed();

        if (Mathf.Abs(moveInput) > 0.1f)
            SetFacing(Mathf.Sign(moveInput));

        TickCoyoteTimer();
        TickJumpBuffer();
        TickWallCoyoteTimer();
    }

    // =========================================================================
    // FIXED UPDATE — physics
    // =========================================================================

    private void FixedUpdate()
    {
        // ── Detect ground & walls ─────────────────────────────────────────────
        wasGrounded        = isGrounded;
        isGrounded         = CheckGround();
        isTouchingWallLeft = CheckWallLeft();
        isTouchingWallRight= CheckWallRight();

        // Evaluate whether we're actively clinging to a wall
        EvaluateWallContact();

        // ── Landing ───────────────────────────────────────────────────────────
        if (isGrounded && !wasGrounded)
        {
            isJumping = false;
            if (!hasDash) hasDash = true;
        }

        // Left ground naturally
        if (!isGrounded && wasGrounded && !isDashing)
            coyoteTimer = coyoteTime;

        // ── Dash overrides everything ─────────────────────────────────────────
        if (isDashing)
        {
            rb.gravityScale = 0f;
            return;
        }

        // ── Wall vs normal physics ────────────────────────────────────────────
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

    // =========================================================================
    // CHECK POINTS
    // =========================================================================

    private bool CheckGround()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
    }

    private bool CheckWallLeft()
    {
        if (wallCheckLeft == null) return false;
        return Physics2D.OverlapCircle(wallCheckLeft.position, checkRadius, groundLayer);
    }

    private bool CheckWallRight()
    {
        if (wallCheckRight == null) return false;
        return Physics2D.OverlapCircle(wallCheckRight.position, checkRadius, groundLayer);
    }

    private bool CheckLedgeLeft()
    {
        if (ledgeCheckLeft == null) return false;
        return Physics2D.OverlapCircle(ledgeCheckLeft.position, checkRadius, groundLayer);
    }

    private bool CheckLedgeRight()
    {
        if (ledgeCheckRight == null) return false;
        return Physics2D.OverlapCircle(ledgeCheckRight.position, checkRadius, groundLayer);
    }

    // =========================================================================
    // WALL CONTACT EVALUATION
    // =========================================================================

    /// <summary>
    /// isOnWall is true only when the player is:
    ///   · holding Grab
    ///   · touching a wall (left or right check point hit)
    /// </summary>
    private void EvaluateWallContact()
    {
        bool touchingAnyWall = isTouchingWallLeft || isTouchingWallRight;
        bool wasOnWall       = isOnWall;

        // 2. GROUND GRAB FIX: Removed the !isGrounded check so player can grab from the floor.
        isOnWall = grabHeld && touchingAnyWall;

        if (isOnWall)
        {
            // Determine which side — right wall takes priority if both somehow fire
            wallDir = isTouchingWallRight ? 1f : -1f;
            SetFacing(-wallDir);
        }
        else if (wasOnWall)
        {
            // Just left the wall — open coyote window
            wallCoyoteTimer = wallCoyoteTime;
        }
    }

    // =========================================================================
    // WALL CLIMBING & LEDGE VAULTING
    // =========================================================================

    /// <summary>
    /// Drives vertical movement while clinging to a wall.
    /// </summary>
    private void HandleWallClimbing()
    {
        rb.gravityScale = 0f;

        float vy = 0f;
        if (verticalInput > 0.3f)
            vy = wallClimbSpeed;
        else if (verticalInput < -0.3f && !isGrounded) // Prevent digging into the ground if at bottom of wall
            vy = -wallSlideSpeed;

        rb.linearVelocity = new Vector2(0f, vy);

        // Automatic Ledge Vaulting check
        if (verticalInput > 0.3f)
        {
            if (wallDir == -1f && isTouchingWallLeft && !CheckLedgeLeft())
            {
                PushOntoLedge(-1f);
            }
            else if (wallDir == 1f && isTouchingWallRight && !CheckLedgeRight())
            {
                PushOntoLedge(1f);
            }
        }
    }

    /// <summary>
    /// Executes the physical push to mount a platform when reaching the top.
    /// </summary>
    private void PushOntoLedge(float dir)
    {
        isOnWall = false;
        
        // Small positional bump to clear the lip of the wall and avoid friction stalling
        transform.position += new Vector3(dir * 0.15f, 0.2f, 0f);
        
        // Push the player up and over
        rb.linearVelocity = new Vector2(dir * ledgeClimbPush.x, ledgeClimbPush.y);
    }

    // =========================================================================
    // WALL JUMP
    // =========================================================================

    private void TryConsumeWallJumpBuffer()
    {
        bool canWallJump = isOnWall || wallCoyoteTimer > 0f;
        if (jumpBufferTimer <= 0f || !canWallJump) return;

        // Is the player pushing INTO the wall?
        bool pushingIntoWall = Mathf.Abs(moveInput) > 0.1f &&
                               Mathf.Sign(moveInput) == wallDir;

        if (pushingIntoWall)
        {
            // Climb-jump: straight up
            rb.linearVelocity = new Vector2(0f, wallJumpVerticalSpeed);
        }
        else
        {
            // Push-away: launch diagonally away from the wall
            float pushDir = -wallDir;
            rb.linearVelocity = new Vector2(pushDir * wallJumpHorizontalSpeed,
                                            wallJumpVerticalSpeed);
            SetFacing(pushDir);
            hasDash = true;   // wall-jump restores dash
        }

        jumpBufferTimer  = 0f;
        wallCoyoteTimer  = 0f;
        isOnWall         = false;
        isJumping        = true;
        rb.gravityScale  = gravityScale;
    }

    // =========================================================================
    // SPRITE DIRECTION
    // =========================================================================

    private void SetFacing(float dir)
    {
        if (Mathf.Approximately(dir, facingDir)) return;
        facingDir = dir;
        if (spriteRenderer != null)
            spriteRenderer.flipX = dir < 0f;
    }

    // =========================================================================
    // HORIZONTAL MOVEMENT
    // =========================================================================

    private void HandleHorizontalMovement()
    {
        float vx    = rb.linearVelocity.x;
        float accel = isGrounded ? groundAcceleration : groundAcceleration * airAccelerationMultiplier;
        float decel = isGrounded ? groundDeceleration : groundDeceleration * airDecelerationMultiplier;

        if (Mathf.Abs(moveInput) > 0.01f)
            vx = Mathf.MoveTowards(vx, moveInput * maxSpeed, accel * Time.fixedDeltaTime);
        else
            vx = Mathf.MoveTowards(vx, 0f, decel * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
    }

    // =========================================================================
    // COYOTE TIME & JUMP BUFFERING
    // =========================================================================

    private void TickCoyoteTimer()
    {
        if (coyoteTimer > 0f) coyoteTimer -= Time.deltaTime;
    }

    private void TickJumpBuffer()
    {
        if (jumpBufferTimer > 0f) jumpBufferTimer -= Time.deltaTime;
    }

    private void TickWallCoyoteTimer()
    {
        if (wallCoyoteTimer > 0f) wallCoyoteTimer -= Time.deltaTime;
    }

    private void TryConsumeJumpBuffer()
    {
        if (jumpBufferTimer > 0f && (isGrounded || coyoteTimer > 0f))
            ExecuteJump();
    }

    private void ExecuteJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVelocity);
        jumpBufferTimer   = 0f;
        coyoteTimer       = 0f;
        isJumping         = true;
    }

    // =========================================================================
    // GRAVITY MODIFIERS
    // =========================================================================

    private void ApplyGravityModifiers()
    {
        float vy     = rb.linearVelocity.y;
        bool  atApex = isJumping && jumpHeld && vy > 0f && Mathf.Abs(vy) < apexThreshold;

        if      (atApex)  rb.gravityScale = apexGravityScale;
        else if (vy < 0f) rb.gravityScale = fallGravityScale;
        else              rb.gravityScale = gravityScale;
    }

    // =========================================================================
    // CORNER CORRECTION
    // =========================================================================

    private void TryCornerCorrection(Collision2D collision)
    {
        if (rb.linearVelocity.y <= 0f) return;

        bool hittingCeiling = false;
        foreach (ContactPoint2D c in collision.contacts)
            if (c.normal.y < -0.5f) { hittingCeiling = true; break; }
        if (!hittingCeiling) return;

        Bounds b      = col.bounds;
        float  rayLen = b.size.y * 0.5f;

        for (int i = 1; i <= cornerCorrectionRays; i++)
        {
            float   nudge  = cornerCorrectionWidth / cornerCorrectionRays * i;
            Vector2 origin = new Vector2(b.min.x + nudge, b.max.y - 0.01f);
            if (!Physics2D.Raycast(origin, Vector2.up, rayLen, groundLayer))
            {
                transform.position += new Vector3(nudge, 0f, 0f);
                return;
            }
        }

        for (int i = 1; i <= cornerCorrectionRays; i++)
        {
            float   nudge  = cornerCorrectionWidth / cornerCorrectionRays * i;
            Vector2 origin = new Vector2(b.max.x - nudge, b.max.y - 0.01f);
            if (!Physics2D.Raycast(origin, Vector2.up, rayLen, groundLayer))
            {
                transform.position -= new Vector3(nudge, 0f, 0f);
                return;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) => TryCornerCorrection(collision);
    private void OnCollisionStay2D(Collision2D collision)  => TryCornerCorrection(collision);

    // =========================================================================
    // LEDGE POPPING
    // =========================================================================

    private void TryLedgePop()
    {
        // 3. FALLING BUG FIX: rb.linearVelocity.y <= 0f ensures we don't snap back UP onto a ledge while falling down past it.
        if (isGrounded || rb.linearVelocity.y <= 0f || Mathf.Abs(rb.linearVelocity.x) < 0.1f) return;

        Bounds b    = col.bounds;
        float  dir  = Mathf.Sign(rb.linearVelocity.x);
        float  side = dir > 0f ? b.max.x : b.min.x;

        RaycastHit2D hit = Physics2D.Raycast(
            new Vector2(side, b.min.y + 0.01f) + Vector2.up * ledgePopHeight,
            Vector2.down, ledgePopHeight, groundLayer);

        if (hit.collider != null)
        {
            float pop = hit.point.y - b.min.y;
            if (pop > 0f && pop <= ledgePopHeight)
            {
                transform.position += new Vector3(0f, pop, 0f);
                rb.linearVelocity   = new Vector2(rb.linearVelocity.x, 0f);
            }
        }
    }

    // =========================================================================
    // DASHING
    // =========================================================================

    private IEnumerator DashRoutine(float dir)
    {
        hasDash       = false;
        isDashing     = true;
        dashDirection = dir;

        SetFacing(dir);

        if (dashHitStop > 0f)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(dashHitStop);
            Time.timeScale = 1f;
        }

        rb.gravityScale   = 0f;
        rb.linearVelocity = dashZerosVerticalVelocity
            ? new Vector2(dir * dashSpeed, 0f)
            : new Vector2(dir * dashSpeed, rb.linearVelocity.y);

        coyoteTimer = 0f;

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            rb.linearVelocity = new Vector2(dir * dashSpeed,
                dashZerosVerticalVelocity ? 0f : rb.linearVelocity.y);
            elapsed += Time.deltaTime;
            yield return null;
        }

        float vx = Mathf.Clamp(rb.linearVelocity.x, -dashEndSpeedCap, dashEndSpeedCap);
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
        rb.gravityScale   = gravityScale;
        isDashing         = false;
    }

    // =========================================================================
    // EDITOR GIZMOS
    // =========================================================================

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Ground check point — green
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : new Color(0f, 1f, 0f, 0.4f);
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }

        // Wall check left — orange
        if (wallCheckLeft != null)
        {
            Gizmos.color = isTouchingWallLeft
                ? new Color(1f, 0.5f, 0f)
                : new Color(1f, 0.5f, 0f, 0.4f);
            Gizmos.DrawWireSphere(wallCheckLeft.position, checkRadius);
        }

        // Wall check right — orange
        if (wallCheckRight != null)
        {
            Gizmos.color = isTouchingWallRight
                ? new Color(1f, 0.5f, 0f)
                : new Color(1f, 0.5f, 0f, 0.4f);
            Gizmos.DrawWireSphere(wallCheckRight.position, checkRadius);
        }

        // Ledge check left — purple
        if (ledgeCheckLeft != null)
        {
            Gizmos.color = new Color(0.5f, 0f, 1f, 0.7f);
            Gizmos.DrawWireSphere(ledgeCheckLeft.position, checkRadius);
        }

        // Ledge check right — purple
        if (ledgeCheckRight != null)
        {
            Gizmos.color = new Color(0.5f, 0f, 1f, 0.7f);
            Gizmos.DrawWireSphere(ledgeCheckRight.position, checkRadius);
        }

        if (col == null) col = GetComponent<Collider2D>();
        Bounds b = col.bounds;

        // Corner correction — cyan
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(b.min.x, b.max.y),
                        new Vector3(b.min.x - cornerCorrectionWidth, b.max.y));
        Gizmos.DrawLine(new Vector3(b.max.x, b.max.y),
                        new Vector3(b.max.x + cornerCorrectionWidth, b.max.y));

        // Ledge pop — yellow
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(b.min.x, b.min.y),
                        new Vector3(b.min.x, b.min.y + ledgePopHeight));
        Gizmos.DrawLine(new Vector3(b.max.x, b.min.y),
                        new Vector3(b.max.x, b.min.y + ledgePopHeight));

        // Dash direction — magenta
        if (Application.isPlaying && isDashing)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(b.center, new Vector3(dashDirection * 1.5f, 0f));
        }
    }
#endif
}