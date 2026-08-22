using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class CelesteMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed = 9f; // Kecepatan maksimal berlari
    [SerializeField] private float acceleration = 40f; // Seberapa cepat mencapai maxSpeed
    [SerializeField] private float deceleration = 40f; // Seberapa cepat berhenti saat tombol dilepas
    [SerializeField] private float turnSpeed = 80f; // Seberapa cepat berbalik arah saat sedang berlari

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;

    private Rigidbody2D rb;
    private float horizontalInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Memastikan Rigidbody tidak berputar
        rb.freezeRotation = true;
    }

    private void OnEnable()
    {
        moveAction?.action.Enable();
    }

    private void OnDisable()
    {
        moveAction?.action.Disable();
    }

    private void Update()
    {
        // Membaca input (-1 untuk kiri, 1 untuk kanan, 0 untuk diam)
        horizontalInput = moveAction == null
            ? 0f
            : moveAction.action.ReadValue<Vector2>().x;

        Debug.Log("Input Horizontal: " + horizontalInput);
    }

    private void FixedUpdate()
    {
        // 1. Tentukan kecepatan target berdasarkan input
        float targetSpeed = horizontalInput * maxSpeed;

        // 2. Tentukan rate perubahan (akselerasi, deselerasi, atau berbalik arah)
        float accelRate;

        if (Mathf.Abs(targetSpeed) > 0.01f) // Jika pemain menekan tombol (ada input)
        {
            // Cek apakah pemain mencoba berbalik arah (input berlawanan dengan arah gerak saat ini)
            if (Mathf.Sign(targetSpeed) != Mathf.Sign(rb.linearVelocity.x) && Mathf.Abs(rb.linearVelocity.x) > 0.01f)
            {
                accelRate = turnSpeed; // Gunakan kecepatan putar balik yang lebih tinggi
            }
            else
            {
                accelRate = acceleration; // Sedang berlari maju
            }
        }
        else // Jika pemain melepas tombol
        {
            accelRate = deceleration;
        }

        // 3. Aplikasikan perubahan kecepatan secara perlahan namun pasti menggunakan MoveTowards
        // Ini mencegah karakter bablas (overshoot) dan memberikan micro-slide yang enak
        float newVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);
    }
}