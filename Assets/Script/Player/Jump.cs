using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class CelesteJump : MonoBehaviour
{
    [Header("Jump Settings")]
    [Tooltip("Kekuatan lompatan awal")]
    [SerializeField] private float jumpForce = 12f;
    [Range(0f, 1f)]
    [SerializeField] private float jumpCutMultiplier = 0.5f;

    [Header("Gravity & Fall")]
    [SerializeField] private float defaultGravity = 2f;
    [SerializeField] private float fallGravityMultiplier = 1.5f;
    [SerializeField] private float maxFallSpeed = 20f;

    [Header("Apex Hang Time")]
    [SerializeField] private float apexThreshold = 2f;
    [Range(0f, 1f)]
    [SerializeField] private float apexGravityMultiplier = 0.5f;

    [Header("Assists (Coyote & Buffer)")]
    [Tooltip("Durasi (detik) player masih dianggap 'boleh lompat' setelah kaki lepas dari ground.")]
    [SerializeField] private float coyoteTime = 0.1f;
    [Tooltip("Durasi (detik) input jump 'disimpan' sebelum benar-benar mendarat di ground.")]
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private InputActionReference jumpAction;

    private Rigidbody2D rb;

    // Timers untuk assist system
    private float coyoteTimeCounter;
    private float jumpBufferCounter;

    // <===== UNITY LIFECYCLE =====>

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
        UpdateCoyoteTime();
        UpdateJumpBuffer();
        HandleJumpCut();
    }

    private void FixedUpdate()
    {
        TryExecuteJump();
        ApplyGravity();
    }

    // ============================================================
    // COYOTE TIME
    // Ide: di platformer, kalau player lompat SEDIKIT setelah kakinya
    // udah lepas dari tepi platform, rasanya "harusnya bisa" tapi gagal.
    // Coyote time ngasih toleransi waktu (default 0.1s) di mana player
    // masih dianggap "grounded" walau sebenarnya udah di udara, supaya
    // lompatan yang sedikit telat tetap kerasa fair/enak dikontrol.
    // ============================================================
    private void UpdateCoyoteTime()
    {
        if (IsGrounded())
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    // ============================================================
    // JUMP BUFFER
    // Ide kebalikan dari coyote time: kalau player pencet tombol jump
    // SEDIKIT SEBELUM mendarat (misal 1-2 frame sebelum kaki nyentuh
    // ground), inputnya jangan dibuang begitu saja. Buffer nyimpen
    // input itu selama jumpBufferTime detik, jadi begitu benar-benar
    // mendarat, lompatan langsung tereksekusi tanpa player harus
    // pencet ulang dengan timing sempurna.
    // ============================================================
    private void UpdateJumpBuffer()
    {
        if (jumpAction == null) return;

        if (jumpAction.action.WasPressedThisFrame())
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    // ============================================================
    // JUMP CUT (Variable Jump Height)
    // Ide: kalau tombol jump dilepas SAAT karakter masih naik (velocity.y > 0),
    // potong velocity.y-nya jadi sebagian kecil (jumpCutMultiplier).
    // Efeknya: tap cepat = lompatan pendek, tahan tombol = lompatan penuh.
    // Ini yang bikin player punya kontrol atas tinggi lompatan, bukan
    // cuma satu ukuran lompatan yang fixed.
    // ============================================================
    private void HandleJumpCut()
    {
        if (jumpAction == null) return;

        if (jumpAction.action.WasReleasedThisFrame() && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);

            // Reset coyote time supaya lepas tombol di udara nggak bisa
            // dianggap kesempatan buat lompat lagi (mencegah double jump tak sengaja).
            coyoteTimeCounter = 0f;
        }
    }

    // ============================================================
    // EXECUTE JUMP
    // Lompatan baru benar-benar dieksekusi kalau KEDUA syarat terpenuhi:
    // - jumpBufferCounter > 0  -> ada input jump yang masih "berlaku"
    // - coyoteTimeCounter > 0  -> player masih dianggap boleh lompat
    // Begitu tereksekusi, kedua counter di-reset ke 0 supaya satu input
    // cuma menghasilkan satu lompatan (nggak nembak berkali-kali).
    // ============================================================
    private void TryExecuteJump()
    {
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }
    }

    // ============================================================
    // GRAVITY: NAIK / APEX / JATUH
    // Tiga fase gravity yang beda biar lompatan kerasa "punya karakter"
    // alih-alih parabola matematis biasa:
    //
    // 1. JATUH (velocity.y < 0): gravity diperberat (fallGravityMultiplier)
    //    biar jatuhnya snappy/cepat, plus dibatasi maxFallSpeed supaya
    //    nggak jatuh makin lama makin ngebut tanpa batas.
    //
    // 2. APEX / PUNCAK (velocity.y mendekati 0, dalam rentang apexThreshold):
    //    gravity dikurangi (apexGravityMultiplier) sesaat, bikin karakter
    //    serasa "melayang" sebentar di titik tertinggi lompatan sebelum
    //    mulai jatuh lagi. Ini ciri khas platformer presisi kayak Celeste -
    //    ngasih player window waktu ekstra buat koreksi posisi di udara.
    //
    // 3. NAIK NORMAL (velocity.y > apexThreshold): gravity default.
    // ============================================================
    private void ApplyGravity()
    {
        bool isFalling = rb.linearVelocity.y < 0f;
        bool isAtApex = Mathf.Abs(rb.linearVelocity.y) < apexThreshold;

        if (isFalling)
        {
            rb.gravityScale = defaultGravity * fallGravityMultiplier;

            if (rb.linearVelocity.y < -maxFallSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
            }
        }
        else if (isAtApex)
        {
            rb.gravityScale = defaultGravity * apexGravityMultiplier;
        }
        else
        {
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