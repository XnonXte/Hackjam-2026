using System.Collections;
using UnityEngine;

/// <summary>
/// Handles player death and respawning.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(PlayerController), typeof(Collider2D))]
public class DeathManager : MonoBehaviour
{
    [Header("Respawn Settings")]
    [SerializeField] private Transform currentSpawnPoint;
    
    [Tooltip("Time to wait before respawning. Set this slightly longer than your death animation.")]
    [SerializeField] private float respawnDelay = 1.0f;

    [Header("Hazard Detection")]
    [SerializeField] private LayerMask hazardLayer;

    [Header("Camera Bounds")]
    [SerializeField] private bool dieOnCameraBottom = true;

    private Rigidbody2D rb;
    private Collider2D col;
    private PlayerController player;
    private Camera mainCamera;
    private bool isRespawning = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        player = GetComponent<PlayerController>();
    }

    private void Start()
    {
        GameObject camObj = GameObject.FindGameObjectWithTag("MainCamera");
        if (camObj != null)
        {
            mainCamera = camObj.GetComponent<Camera>();
        }
        else
        {
            Debug.LogError("DeathManager: No GameObject found with the tag 'MainCamera'!");
        }
    }

    private void Update()
    {
        if (dieOnCameraBottom && !isRespawning)
        {
            CheckBottomBoundary();
        }
    }

    private void CheckBottomBoundary()
    {
        if (mainCamera == null) return;

        // Get the bottom-left world coordinate of the camera to find the bottom Y edge
        Vector3 cameraBottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, mainCamera.nearClipPlane));

        // Bottom Kill Line
        // Checks if the absolute bottom edge of the player's collider goes below the camera's bottom edge
        if (col.bounds.min.y <= cameraBottomLeft.y)
        {
            Die();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isRespawning && IsHazard(collision.gameObject.layer))
        {
            Die();
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (!isRespawning && IsHazard(collider.gameObject.layer))
        {
            Die();
        }
    }

    private bool IsHazard(int layerIndex)
    {
        return (hazardLayer.value & (1 << layerIndex)) > 0;
    }

    private void Die()
    {
        if (isRespawning) return;
        
        player.Die();
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        isRespawning = true;
        
        yield return new WaitForSeconds(respawnDelay);

        if (currentSpawnPoint != null)
        {
            transform.position = currentSpawnPoint.position;
            rb.linearVelocity = Vector2.zero; 
        }

        player.Revive();
        isRespawning = false;
    }

    public void SetSpawnPoint(Transform newSpawnPoint)
    {
        currentSpawnPoint = newSpawnPoint;
    }
}