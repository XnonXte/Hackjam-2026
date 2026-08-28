using UnityEngine;

public class CokeController : MonoBehaviour
{
    [Header("Identitas Coke")]
    [Tooltip("ID Harus Unique untuk setiap Coke")]
    public string cokeID;

    [Header("Floating Animation")]
    [SerializeField] private float floatAmplitude = 0.2f;
    [SerializeField] private float floatFrequency = 3f;

    [Header("SFX")]
    [SerializeField] private AudioClip collectSound;

    [Header("Visual Effects")]
    [SerializeField] private GameObject pickupEffectPrefab;

    private Vector3 startPos;

    private SpriteRenderer spriteRenderer;
    private Collider2D cokeCollider;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        cokeCollider = GetComponent<BoxCollider2D>();
    }

    private void OnEnable()
    {
        DeathManager.OnDied += RefreshCondition;
    }

    private void OnDisable()
    {
        DeathManager.OnDied -= RefreshCondition;
    }

    private void Start()
    {
        startPos = transform.position;
        cokeCollider.isTrigger = true;

        RefreshCondition();
    }

    private void Update()
    {
        if (!spriteRenderer.enabled) return;

        float newY = startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }

    public void RefreshCondition()
    {
        // Cek data permanen untuk level normal
        if (DataManager.IsCokeAlreadyCollected(GameManager.Instance.levelID, cokeID))
        {
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
            spriteRenderer.enabled = true;
            cokeCollider.enabled = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerController player = collision.GetComponent<PlayerController>();

        if (player != null && !player.IsDead)
        {
            if (collectSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFXAtPosition(collectSound, transform.position);
            }

            // --- SPAWN ANIMASI DI SINI ---
            if (pickupEffectPrefab != null)
            {
                Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
            }

            // 2. BLOKIR PENYIMPANAN: Jangan kirim ke SessionManager jika ini tutorial (Level 0)
            if (GameManager.Instance.levelID != 0)
            {
                GameSessionManager.Instance.AddCoke(cokeID);
            }

            spriteRenderer.enabled = false;
            cokeCollider.enabled = false;
        }
    }
}