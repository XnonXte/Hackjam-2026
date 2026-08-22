using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class CelesteJump : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 12f;
    [Range(0f, 1f)]
    [SerializeField] private float jumpCutMultiplier = 0.5f;

    [Header("Gravity & Fall")]
    [SerializeField] private float defaultGravity = 2f;
    [SerializeField] private float fallGravityMultiplier = 1.5f; // Gravitasi lebih berat saat jatuh
    [SerializeField] private float maxFallSpeed = 20f; // Batas kecepatan jatuh maksimal

    [Header("Assists (Coyote & Buffer)")]
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private InputActionReference jumpAction;

    private Rigidbody2D rb;

    // Timers
    private float coyoteTimeCounter;
    private float jumpBufferCounter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = defaultGravity;
    }

    private void OnEnable()
    {
        jumpAction?.action.Enable();
    }

    private void OnDisable()
    {
        jumpAction?.action.Disable();
    }

    private void Update()
    {
        // 1. COYOTE TIME LOGIC
        if (IsGrounded())
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (jumpAction != null)
        {
            // 2. JUMP BUFFER LOGIC
            if (jumpAction.action.WasPressedThisFrame())
            {
                jumpBufferCounter = jumpBufferTime;
            }
            else
            {
                jumpBufferCounter -= Time.deltaTime;
            }

            // 3. JUMP CUT (Variable Jump Height)
            if (jumpAction.action.WasReleasedThisFrame() && rb.linearVelocity.y > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
                coyoteTimeCounter = 0f; // Mencegah lompatan ganda di udara
            }
        }
    }

    private void FixedUpdate()
    {
        // 4. EXECUTE JUMP
        // Jika ada input lompat yang tersimpan (Buffer) DAN kita masih punya waktu toleransi (Coyote)
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            // Reset timer agar lompatan tidak tereksekusi berkali-kali
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }

        // 5. CUSTOM GRAVITY & FALL SPEED
        if (rb.linearVelocity.y < 0)
        {
            // Jatuh lebih cepat (snappy)
            rb.gravityScale = defaultGravity * fallGravityMultiplier;

            // Batasi kecepatan jatuh maksimum (Terminal Velocity)
            if (rb.linearVelocity.y < -maxFallSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
            }
        }
        else
        {
            // Gravitasi normal saat bergerak naik
            rb.gravityScale = defaultGravity;
        }
    }

    private bool IsGrounded()
    {
        if (groundCheck == null) return false;

        return Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer) != null;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}