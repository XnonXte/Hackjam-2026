using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CelesteMovement))]
public class DashMechanic : MonoBehaviour
{
    public enum DashType
    {
        FourWay,
        EightWay,
    }

    [Header("Dash Settings")]
    [SerializeField] private DashType dashType = DashType.FourWay;
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashMomentumMultiplier = 0.3f;
    [SerializeField] private InputActionReference dashAction;

    [Header("Ground Check")] // Untuk Reset Dash
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private CelesteMovement movementScript;

    private bool isDashing;
    private bool canDash = true;
    private float defaultGravity;
    private int facingDirection = 1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movementScript = GetComponent<CelesteMovement>();
        defaultGravity = rb.gravityScale;
    }

    private void OnEnable()
    {
        dashAction?.action.Enable();
    }

    private void OnDisable()
    {
        dashAction?.action.Disable();
    }

    private void Update()
    {
        float hInput = movementScript.GetHorizontalInput();
        if (hInput > 0.01f) facingDirection = 1;
        else if (hInput < -0.01f) facingDirection = -1;

        if (IsGrounded() && !isDashing)
        {
            canDash = true;
        }

        if (dashAction != null && dashAction.action.WasPressedThisFrame())
        {
            if (canDash && !isDashing)
            {
                StartCoroutine(PerformDash());
            }
        }
    }

    private IEnumerator PerformDash()
    {
        isDashing = true;
        canDash = false;

        movementScript.LockMovement(dashDuration);
        Vector2 dashDirection = GetDashDirection();

        rb.gravityScale = 0f;
        rb.linearVelocity = dashDirection * dashSpeed;

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = defaultGravity;
        rb.linearVelocity *= dashMomentumMultiplier;

        isDashing = false;
    }

    private Vector2 Get4WayDashDirection()
    {
        float hInput = movementScript.GetHorizontalInput();
        float vInput = movementScript.GetVerticalInput();

        // Jika pemain menekan tombol arah
        if (Mathf.Abs(hInput) > 0.1f || Mathf.Abs(vInput) > 0.1f)
        {
            // Bandingkan mana tarikan yang lebih kuat (X atau Y)
            if (Mathf.Abs(hInput) > Mathf.Abs(vInput))
            {
                // Tarikan horizontal dominan (Kiri / Kanan)
                return new Vector2(Mathf.Sign(hInput), 0f);
            }
            else
            {
                // Tarikan vertikal dominan (Atas / Bawah)
                return new Vector2(0f, Mathf.Sign(vInput));
            }
        }

        // Jika tidak menekan apa-apa, dash ke depan (Horizontal)
        return new Vector2(facingDirection, 0f);
    }

    // Opsi jika ingin dash 8 arah.
    private Vector2 Get8WayDashDirection()
    {
        // 1. Ambil input mentah dari pemain
        float hInput = movementScript.GetHorizontalInput();
        float vInput = movementScript.GetVerticalInput();

        // 2. Masukkan ke dalam satu Vector2
        Vector2 inputVector = new Vector2(hInput, vInput);

        // 3. Jika pemain menekan tombol (vektor tidak nol)
        if (inputVector.sqrMagnitude > 0.01f)
        {
            // normalized memastikan kecepatan diagonal (misal x:1, y:1) tidak lebih cepat dari lurus.
            // Tanpa normalized, diagonal akan bernilai 1.414, membuat dash diagonal melesat terlalu jauh.
            return inputVector.normalized;
        }

        // 4. Jika pemain menekan dash tanpa menekan arah sama sekali, dash ke depan
        return new Vector2(facingDirection, 0f);
    }

    private Vector2 GetDashDirection()
    {
        switch (dashType)
        {
            case DashType.FourWay:
                return Get4WayDashDirection();

            case DashType.EightWay:
                return Get8WayDashDirection();

            default:
                return Vector2.zero;
        }
    }

    private bool IsGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
    }
}