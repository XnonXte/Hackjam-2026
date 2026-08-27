using UnityEngine;

public class CokeController : MonoBehaviour
{
    [Header("Identitas Coke")]
    [Tooltip("ID Harus Unique untuk setiap Coke")]

    [Header("Floating Animation")]
    [SerializeField] private float floatAmplitude = 0.2f;
    [SerializeField] private float floatFrequency = 3f;

    [Header("SFX")]
    [SerializeField] private AudioClip collectSound;

    private Vector3 startPos;
    public string cokeID;

    private void Start()
    {
        startPos = transform.position;
        GetComponent<Collider2D>().isTrigger = true;

        if (DataManager.IsCokeAlreadyCollected(GameManager.Instance.levelID, cokeID))
        {
            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        float newY = startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
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
            GameSessionManager.Instance.AddCoke(cokeID);
            gameObject.SetActive(false);
        }
    }
}