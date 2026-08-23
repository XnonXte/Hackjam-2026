using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class CelesteMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed = 9f;
    [SerializeField] private float acceleration = 40f;
    [SerializeField] private float deceleration = 40f;
    [SerializeField] private float turnSpeed = 80f;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;

    private Rigidbody2D rb;
    private float horizontalInput;
    private float lockTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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
        horizontalInput = moveAction == null
            ? 0f
            : moveAction.action.ReadValue<Vector2>().x;
        if (lockTimer > 0)
        {
            lockTimer -= Time.deltaTime;
        }

        // Debug.Log("Input Horizontal: " + horizontalInput);
    }

    private void FixedUpdate()
    {
        if (lockTimer > 0) return;

        // 1. Kecepatan Move Seharusnya.
        float targetSpeed = horizontalInput * maxSpeed;

        // 2. Akselerasi atau Deselerasi
        float accelRate;

        // Jika Player mencoba Maju.
        if (Mathf.Abs(targetSpeed) > 0.01f)
        {
            // Ini bacanya: Jika targetSpeed memiliki tanda yang berbeda dengan kecepatan saat ini (Player putar balik) dan Player sedang bergerak.
            // Intinya: Jika Player putar balik saat sedang Move.
            if (Mathf.Sign(targetSpeed) != Mathf.Sign(rb.linearVelocity.x) && Mathf.Abs(rb.linearVelocity.x) > 0.01f)
            {
                // Percepatan putar balik.
                accelRate = turnSpeed;
            }
            else
            {
                // Percepatan normal.
                accelRate = acceleration;
            }
        }
        else
        {
            accelRate = deceleration;
        }

        // 3. Aplikasikan perubahan kecepatan secara perlahan namun pasti menggunakan MoveTowards
        // Ini mencegah karakter bablas (overshoot) dan memberikan micro-slide yang enak
        float newVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);
    }

    //  <====== PUBLIC METHODS =====>
    public float GetVerticalInput()
    {
        return moveAction == null ? 0f : moveAction.action.ReadValue<Vector2>().y;
    }
    public void LockMovement(float duration)
    {
        lockTimer = duration;
    }
    public float GetHorizontalInput()
    {
        return horizontalInput;
    }
}