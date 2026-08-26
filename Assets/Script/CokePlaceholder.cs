using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Collectible : MonoBehaviour
{
    [Header("Floating Animation")]
    [SerializeField] private float floatAmplitude = 0.2f;
    [SerializeField] private float floatFrequency = 3f;

    private Vector3 startPos;

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
        // Check if the object that entered the trigger has your PlayerController
        PlayerController player = collision.GetComponent<PlayerController>();

        // Ensure it's the player and they aren't dead before collecting
        if (player != null && !player.IsDead)
        {
            Debug.Log("Strawberry collected! (Or whatever this is)");
            
            // Deactivate the collectible so it disappears from the scene
            gameObject.SetActive(false);
            
            // Note: If you want to permanently delete it, you can use Destroy(gameObject) instead.
            // SetActive(false) is often better if you plan to respawn the player/level without reloading the scene.
        }
    }
}