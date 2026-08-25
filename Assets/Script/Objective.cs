using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Objective : MonoBehaviour
{
    [Header("Floating Animation")]
    [SerializeField] private float floatAmplitude = 0.25f;
    [SerializeField] private float floatFrequency = 2f;

    private Vector3 startPos;
    private bool isCompleted = false;

    private void Start()
    {
        startPos = transform.position;
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Update()
    {
        // Stop floating once the level is completed (optional, but looks nice)
        if (isCompleted) return;

        float newY = startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Prevent multiple triggers in the same frame or after completion
        if (isCompleted) return;

        PlayerController player = collision.GetComponent<PlayerController>();

        if (player != null && !player.IsDead)
        {
            isCompleted = true;
            Debug.Log("Objective reached! Level Complete!");
            
            // You can eventually hook this up to a Game Manager to load the next scene
            // e.g., GameManager.Instance.CompleteLevel();
        }
    }
}