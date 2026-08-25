using UnityEngine;

public class CokeController : MonoBehaviour
{
    [Header("Identitas Coke")]
    [Tooltip("ID Harus Unique untuk setiap Coke")]

    [Header("Floating Animation")]
    [SerializeField] private float floatAmplitude = 0.2f;
    [SerializeField] private float floatFrequency = 3f;

    private Vector3 startPos;
    public string cokeID;

    private void Start()
    {
        // Record the initial position to base the mathematical floating on
        startPos = transform.position;

        // Ensure the collider is set as a trigger so it doesn't block player movement
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Update()
    {
        // Calculate the new Y position using a Sine wave
        float newY = startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerController player = collision.GetComponent<PlayerController>();

        // Ensure it's the player and they aren't dead before collecting
        if (player != null && !player.IsDead)
        {
            GameSessionManager.Instance.AddCoke(cokeID);
            gameObject.SetActive(false);
            // AudioManager.Play("GetItem");
        }
    }
}