    using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    // =====================================================================
    // INSPECTOR FIELDS
    // =====================================================================

    [Header("Basic Movement")]
    public float moveSpeed           = 8f;
    public float groundAcceleration  = 80f;
    public float groundDeceleration  = 80f;
    public float airAcceleration     = 60f;
    public float airDeceleration     = 60f;

    [Header("Gravity")]
    [Tooltip("The gravity scale that is always used as the resting value. " +
             "Temporary overrides (dash, wall-grab) restore to this.")]
    public float baseGravityScale      = 4f;

    [Tooltip("Multiplier applied on top of baseGravityScale while the player " +
             "is falling (vy < 0) and not dashing / grabbing.")]
    public float fallGravityMultiplier = 1.5f;

    [Tooltip("Multiplier applied on top of baseGravityScale while the player " +
             "is rising (vy > 0) after a jump.")]
    public float jumpGravityMultiplier = 1f;

    [Header("Jump")]
    public float jumpForce         = 14f;

    [Range(0f, 1f)]
    public float jumpCutMultiplier = 0.45f;

    public float coyoteTime      = 0.12f;
    public float jumpBufferTime  = 0.12f;

    [Header("Dash")]
    public float dashSpeed    = 24f;
    public float dashDuration = 0.15f;

    [Header("Wall Mechanics")]
    public float   wallClimbSpeed  = 4f;
    public float   wallSlideSpeed  = 2f;
    public Vector2 wallJumpForce   = new Vector2(12f, 16f);

    [Tooltip("Minimum time after a wall jump before the player can grab or " +
             "slide on a wall again. Prevents immediately re-clinging to the " +
             "same wall while still inside its check radius.")]
    public float wallRegrabCooldown = 0.25f;

    [Header("Ledge Climb")]
    [Tooltip("How far above the player's centre the open-space check is cast " +
             "(should be roughly half-height + a small margin).")]
    public float ledgeOpenSpaceHeight = 0.6f;

    [Tooltip("Horizontal distance of the ledge surface check, measured from " +
             "the player centre outward toward the wall.")]
    public float ledgeSurfaceReach = 0.6f;

    [Tooltip("Vertical distance searched downward from the open-space point to " +
             "find the ledge top surface.")]
    public float ledgeSurfaceScanDepth = 0.5f;

    [Tooltip("How quickly the player snaps onto the ledge (units / second).")]
    public float ledgeClimbSpeed = 12f;

    [Header("Collision Detection")]
    public Transform groundCheck;
    public Transform leftWallCheck;
    public Transform rightWallCheck;

    public float     checkRadius = 0.12f;
    public LayerMask whatIsGround;

    [Header("Input Actions")]
    public InputActionReference moveAction;
    public InputActionReference jumpAction;
    public InputActionReference dashAction;
    public InputActionReference grabAction;

    // =====================================================================
    // PRIVATE STATE
    // =====================================================================

    private Rigidbody2D rb;

    private Vector2 moveInput;

    // Timers
    private float coyoteTimer;
    private float jumpBufferTimer;

    // Facing: +1 = right, -1 = left
    private int facingDirection = 1;

    // Environment flags
    private bool isGrounded;
    private bool isTouchingLeftWall;
    private bool isTouchingRightWall;
    private bool isTouchingWall;

    // -1 = left wall, 0 = none, +1 = right wall
    private int wallDirection;

    // Action flags
    private bool isDashing;
    private bool isGrabbing;
    private bool isWallSliding;
    private bool canDash = true;

    // Ledge climb coroutine guard.
    private bool isClimbingLedge;

    // Counts down after a wall jump. While > 0, wall grab and wall slide are
    // suppressed so the player can't immediately re-cling to the same wall.
    private float wallRegrabTimer;

    // =====================================================================
    // UNITY LIFECYCLE
    // =====================================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Initialise the physics gravity from the Inspector value so the
        // Rigidbody2D never silently defaults to 1.
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

        // Jump must run before wall mechanics so PerformWallJump() can start
        // the regrab cooldown before HandleWallMechanics() evaluates.
        HandleJump();
        HandleWallMechanics();
        HandleDash();
        HandleFacingDirection();
    }

    private void FixedUpdate()
    {
        if (isDashing || isClimbingLedge)
            return;

        if (!isGrabbing)
        {
            ApplyMovement();
            ApplyGravityScale();
        }
    }

    // =====================================================================
    // ENVIRONMENT
    // =====================================================================

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

        isTouchingWall = isTouchingLeftWall || isTouchingRightWall;

        // Determine which wall the player is touching, independent of facing.
        if (isTouchingLeftWall)
            wallDirection = -1;
        else if (isTouchingRightWall)
            wallDirection = 1;
        else
            wallDirection = 0;

        // Coyote time: refresh while grounded.
        if (isGrounded)
        {
            coyoteTimer = coyoteTime;

            // Ground is the ONLY place dash is refilled.
            canDash = true;
        }

        // NOTE: Wall touch / grab deliberately does NOT refill dash here.
        //       Requirement 6: ground-only dash refill.
    }

    private void UpdateTimers()
    {
        if (!isGrounded)
            coyoteTimer -= Time.deltaTime;

        if (jumpBufferTimer > 0f)
            jumpBufferTimer -= Time.deltaTime;

        if (wallRegrabTimer > 0f)
            wallRegrabTimer -= Time.deltaTime;
    }

    // =====================================================================
    // GRAVITY
    // =====================================================================

    /// <summary>
    /// Applies the correct gravity multiplier each FixedUpdate based on
    /// whether the player is rising or falling. Temporary overrides
    /// (dash, wall-grab) bypass this method entirely.
    /// </summary>
    private void ApplyGravityScale()
    {
        if (isDashing || isGrabbing)
            return; // Those systems manage gravity themselves.

        float multiplier = rb.linearVelocity.y < 0f
            ? fallGravityMultiplier
            : jumpGravityMultiplier;

        rb.gravityScale = baseGravityScale * multiplier;
    }

    // =====================================================================
    // MOVEMENT
    // =====================================================================

    private void ApplyMovement()
    {
        float targetSpeed = moveInput.x * moveSpeed;
        bool  hasInput    = Mathf.Abs(moveInput.x) > 0.01f;

        float acceleration = isGrounded
            ? (hasInput ? groundAcceleration : groundDeceleration)
            : (hasInput ? airAcceleration    : airDeceleration);

        float velocityX = Mathf.MoveTowards(
            rb.linearVelocity.x,
            targetSpeed,
            acceleration * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector2(velocityX, rb.linearVelocity.y);
    }

    private void HandleFacingDirection()
    {
        if (Mathf.Abs(moveInput.x) < 0.01f)
            return;

        int direction = moveInput.x > 0f ? 1 : -1;

        if (direction != facingDirection)
            Flip();
    }

    // =====================================================================
    // JUMP
    // =====================================================================

    private void HandleJump()
    {
        // Buffer the jump input.
        if (jumpAction.action.WasPressedThisFrame())
            jumpBufferTimer = jumpBufferTime;

        // Consume the buffered jump.
        if (jumpBufferTimer > 0f)
        {
            if (coyoteTimer > 0f)
            {
                PerformGroundJump();
            }
            else if (isTouchingWall)
            {
                // Wall jump is allowed even while Grab is held (Requirement 4).
                PerformWallJump();
            }
        }

        // Variable jump height: cut velocity when button released early.
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
        rb.linearVelocity        = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpBufferTimer    = 0f;
        coyoteTimer        = 0f;
    }

    private void PerformWallJump()
    {
        // Jump away from the wall using the physically detected wall side,
        // not the facing direction (Requirement 3 / 4).
        rb.linearVelocity = new Vector2(
            -wallDirection * wallJumpForce.x,
            wallJumpForce.y
        );

        jumpBufferTimer = 0f;
        coyoteTimer     = 0f;

        // Face away from the wall.
        if (facingDirection != -wallDirection)
            Flip();

        // Release grab state and start the regrab cooldown. This prevents
        // HandleWallMechanics from re-clinging to the same wall while the
        // player is still inside the wall check radius post-jump.
        isGrabbing      = false;
        isWallSliding   = false;
        wallRegrabTimer = wallRegrabCooldown;

        // Restore gravity (wall grab sets it to 0).
        rb.gravityScale = baseGravityScale;
    }

    // =====================================================================
    // WALL MECHANICS
    // =====================================================================

    private void HandleWallMechanics()
    {
        // Suppress wall grab and wall slide while the regrab cooldown is
        // active. This covers the window between the wall jump and the moment
        // the player has physically cleared the wall check radius.
        if (wallRegrabTimer > 0f)
            return;

        bool grabHeld = grabAction.action.IsPressed();

        // --- Wall grab / climb ---
        if (isTouchingWall && grabHeld)
        {
            // On the frame grab is first pressed, snap the collider flush
            // against the wall so there is no visible gap.
            if (!isGrabbing)
                SnapToWall();

            isGrabbing    = true;
            isWallSliding = false;

            // Zero gravity while clinging.
            rb.gravityScale = 0f;

            // Allow vertical movement along the wall.
            rb.linearVelocity = new Vector2(0f, moveInput.y * wallClimbSpeed);

            // Check for automatic ledge climb while grabbing.
            TryLedgeClimb();
        }

        // --- Wall slide ---
        else if (isTouchingWall && !isGrounded && rb.linearVelocity.y < 0f)
        {
            isGrabbing    = false;
            isWallSliding = true;

            rb.gravityScale = baseGravityScale;

            // Cap downward speed.
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                Mathf.Clamp(rb.linearVelocity.y, -wallSlideSpeed, float.MaxValue)
            );
        }

        // --- Normal state ---
        else
        {
            isGrabbing    = false;
            isWallSliding = false;

            // Restore to base; ApplyGravityScale() will apply the fall/rise
            // multiplier in FixedUpdate.
            rb.gravityScale = baseGravityScale;
        }
    }

    // =====================================================================
    // WALL SNAP
    // =====================================================================

    /// <summary>
    /// Moves the player's Rigidbody2D horizontally so the collider sits flush
    /// against the wall surface, eliminating any gap left by the physics
    /// solver. Called once on the frame grab is first pressed.
    /// </summary>
    private void SnapToWall()
    {
        // Cast from the player centre toward the wall to find the exact surface.
        Vector2 origin    = rb.position;
        Vector2 towardWall = new Vector2(wallDirection, 0f);

        // Use the collider half-width to know how far the surface should be.
        CapsuleCollider2D col = GetComponent<CapsuleCollider2D>();
        float halfWidth = col != null ? col.size.x * 0.5f : 0.3f;

        // Cast far enough to reach the wall from any reasonable gap distance.
        float castDistance = halfWidth + 0.5f;

        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            towardWall,
            castDistance,
            whatIsGround
        );

        if (hit.collider == null)
            return;

        // Place the player so the collider edge is exactly on the wall surface.
        float snappedX = hit.point.x - wallDirection * halfWidth;

        rb.MovePosition(new Vector2(snappedX, rb.position.y));
    }

    // =====================================================================
    // LEDGE CLIMB
    // =====================================================================

    /// <summary>
    /// Checks whether the player has reached the top of a wall while
    /// grabbing and, if so, launches the smooth ledge-snap coroutine.
    /// Works symmetrically for both left and right walls.
    /// </summary>
    private void TryLedgeClimb()
    {
        if (isClimbingLedge || wallDirection == 0)
            return;

        Vector2 towardWall   = new Vector2(wallDirection, 0f);
        Vector2 playerCenter = rb.position;

        // --- Step 1: is there open space just above the player? ---
        Vector2 openSpaceOrigin = playerCenter + Vector2.up * ledgeOpenSpaceHeight;

        RaycastHit2D openCheck = Physics2D.Raycast(
            openSpaceOrigin,
            towardWall,
            ledgeSurfaceReach,
            whatIsGround
        );

        // Wall still present at this height — not a ledge edge yet.
        if (openCheck.collider != null)
            return;

        // --- Step 2: scan downward to find the ledge top surface ---
        Vector2 scanOrigin = openSpaceOrigin + towardWall * ledgeSurfaceReach;

        RaycastHit2D ledgeTop = Physics2D.Raycast(
            scanOrigin,
            Vector2.down,
            ledgeSurfaceScanDepth,
            whatIsGround
        );

        if (ledgeTop.collider == null)
            return;

        // --- Step 3: snap the player onto the ledge ---
        CapsuleCollider2D col = GetComponent<CapsuleCollider2D>();
        float halfH = col != null ? col.size.y * 0.5f : 0.5f;
        float halfW = col != null ? col.size.x * 0.5f : 0.3f;

        Vector2 targetPos = new Vector2(
            ledgeTop.point.x - wallDirection * (halfW + checkRadius),
            ledgeTop.point.y + halfH
        );

        StartCoroutine(LedgeClimbRoutine(targetPos));
    }

    private IEnumerator LedgeClimbRoutine(Vector2 target)
    {
        isClimbingLedge = true;
        isGrabbing      = false;

        rb.gravityScale = 0f;
        rb.linearVelocity     = Vector2.zero;

        while (Vector2.Distance(rb.position, target) > 0.02f)
        {
            rb.MovePosition(
                Vector2.MoveTowards(rb.position, target, ledgeClimbSpeed * Time.fixedDeltaTime)
            );
            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(target);

        rb.gravityScale = baseGravityScale;
        isClimbingLedge = false;
    }

    // =====================================================================
    // DASH
    // =====================================================================

    private void HandleDash()
    {
        if (dashAction.action.WasPressedThisFrame() && canDash)
        {
            StartCoroutine(DashRoutine());
        }
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        canDash   = false;          // Stays false until next ground touch.

        rb.gravityScale = 0f;
        rb.linearVelocity     = new Vector2(facingDirection * dashSpeed, 0f);

        yield return new WaitForSeconds(dashDuration);

        // Restore to base gravity; the fall/rise multiplier will be applied
        // each FixedUpdate from now on.
        rb.gravityScale = baseGravityScale;
        isDashing       = false;
    }

    // =====================================================================
    // CHARACTER
    // =====================================================================

    private void Flip()
    {
        facingDirection *= -1;

        Vector3 scale = transform.localScale;
        scale.x      *= -1f;

        transform.localScale = scale;
    }

    // =====================================================================
    // DEBUG GIZMOS
    // =====================================================================

    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }

        if (leftWallCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(leftWallCheck.position, checkRadius);
        }

        if (rightWallCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(rightWallCheck.position, checkRadius);
        }

#if UNITY_EDITOR
        // Ledge-climb ray visualisation (editor only, play mode).
        if (Application.isPlaying && rb != null)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 origin     = rb.position + Vector2.up * ledgeOpenSpaceHeight;
                Vector2 toWall     = new Vector2(side, 0f);
                Vector2 scanOrigin = origin + toWall * ledgeSurfaceReach;

                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(origin, toWall * ledgeSurfaceReach);

                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(scanOrigin, Vector2.down * ledgeSurfaceScanDepth);
            }
        }
#endif
    }
}