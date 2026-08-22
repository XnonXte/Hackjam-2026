using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CelesteMovement))]
public class WallMechanics : MonoBehaviour
{
    [Header("Wall Detection")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private Transform topWallCheck;
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private LayerMask wallLayer;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Wall Slide")]
    [SerializeField] private float wallSlideSpeed = 2f;

    [Header("Wall Grab & Climb")]
    [SerializeField] private float climbSpeed = 3f;
    [SerializeField] private InputActionReference grabAction;

    [Header("Wall Jump")]
    [SerializeField] private Vector2 wallJumpForce = new Vector2(8f, 14f);
    [SerializeField] private float wallJumpLockDuration = 0.15f;
    [SerializeField] private InputActionReference jumpAction;

    [Header("Ledge Vault")]
    [SerializeField] private Vector2 ledgeVaultOffset = new Vector2(0.5f, 0.8f);
    [SerializeField] private float ledgeVaultDuration = 0.15f;

    private Rigidbody2D rb;
    private CelesteMovement movementScript;

    private int facingDirection = 1;
    private float defaultGravity;

    // States
    private bool isWallSliding;
    private bool isGrabbing;
    private bool jumpRequested;
    private bool isLedgeVaulting;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movementScript = GetComponent<CelesteMovement>();
        defaultGravity = rb.gravityScale;
    }

    private void OnEnable()
    {
        jumpAction?.action.Enable();
        grabAction?.action.Enable();
    }

    private void OnDisable()
    {
        jumpAction?.action.Disable();
        grabAction?.action.Disable();
    }

    private void Update()
    {
        if (isLedgeVaulting) return;

        float hInput = movementScript.GetHorizontalInput();
        if (hInput > 0.01f) facingDirection = 1;
        else if (hInput < -0.01f) facingDirection = -1;

        bool isGrabHeld = grabAction != null && grabAction.action.IsPressed();
        float vInput = movementScript.GetVerticalInput();

        // --- CEK LEDGE VAULT ---
        if (isGrabHeld && vInput > 0 && IsWalled() && !IsTopWalled())
        {
            StartCoroutine(PerformLedgeVault());
            return;
        }

        // --- Cek Status Grab Biasa ---
        if (IsWalled() && isGrabHeld)
        {
            isGrabbing = true;
            isWallSliding = false;
        }
        else
        {
            isGrabbing = false;

            if (IsWalled() && !IsGrounded() && rb.linearVelocity.y < 0 && hInput == facingDirection)
            {
                isWallSliding = true;
            }
            else
            {
                isWallSliding = false;
            }
        }

        // --- Input Lompat ---
        if (jumpAction != null && jumpAction.action.WasPressedThisFrame())
        {
            if (isWallSliding || isGrabbing)
            {
                jumpRequested = true;
            }
        }
    }

    private void FixedUpdate()
    {
        if (isLedgeVaulting) return;

        if (isGrabbing)
        {
            rb.gravityScale = 0f;
            float vInput = movementScript.GetVerticalInput();

            if (vInput > 0 && !IsTopWalled())
            {
                vInput = 0f;
                rb.linearVelocity = new Vector2(0f, 0f);
            }
            else
            {
                rb.linearVelocity = new Vector2(0f, vInput * climbSpeed);
            }
        }
        else
        {
            rb.gravityScale = defaultGravity;

            if (isWallSliding)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Clamp(rb.linearVelocity.y, -wallSlideSpeed, float.MaxValue));
            }
        }

        if (jumpRequested)
        {
            movementScript.LockMovement(wallJumpLockDuration);
            float jumpDirection = -facingDirection;
            rb.linearVelocity = new Vector2(wallJumpForce.x * jumpDirection, wallJumpForce.y);
            jumpRequested = false;
        }
    }

    private IEnumerator PerformLedgeVault()
    {
        isLedgeVaulting = true;
        movementScript.LockMovement(ledgeVaultDuration + 0.05f);
        rb.isKinematic = true;
        rb.linearVelocity = Vector2.zero;

        Vector2 startPos = transform.position;
        Vector2 targetPos = startPos + new Vector2(ledgeVaultOffset.x * facingDirection, ledgeVaultOffset.y);
        float elapsedTime = 0f;

        while (elapsedTime < ledgeVaultDuration)
        {
            transform.position = Vector2.Lerp(startPos, targetPos, elapsedTime / ledgeVaultDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;
        rb.isKinematic = false;
        isLedgeVaulting = false;
    }

    private bool IsWalled()
    {
        if (wallCheck == null) return false;
        RaycastHit2D hit = Physics2D.Raycast(wallCheck.position, Vector2.right * facingDirection, wallCheckDistance, wallLayer);
        return hit.collider != null;
    }

    private bool IsTopWalled()
    {
        if (topWallCheck == null) return false;
        RaycastHit2D hit = Physics2D.Raycast(topWallCheck.position, Vector2.right * facingDirection, wallCheckDistance, wallLayer);
        return hit.collider != null;
    }

    // INI DIA FUNGSI YANG HILANG
    private bool IsGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
    }

    private void OnDrawGizmosSelected()
    {
        if (wallCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + (Vector3)(Vector2.right * facingDirection * wallCheckDistance));
        }
        if (topWallCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(topWallCheck.position, topWallCheck.position + (Vector3)(Vector2.right * facingDirection * wallCheckDistance));
        }

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + new Vector3(ledgeVaultOffset.x * facingDirection, ledgeVaultOffset.y, 0));

        // Gizmos tambahan untuk Ground Check agar gampang disetel
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}