using System.Collections;
using UnityEngine;

/// <summary>
/// Handles player death and respawning.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(PlayerController))]
public class DeathManager : MonoBehaviour
{
    [Header("Respawn Settings")]
    [SerializeField] private Transform currentSpawnPoint;
    
    [Tooltip("Time to wait before respawning. Set this slightly longer than your death animation.")]
    [SerializeField] private float respawnDelay = 1.0f;

    [Header("Hazard Detection")]
    [SerializeField] private LayerMask hazardLayer;

    [Header("Camera Bounds")]
    [SerializeField] private bool dieOnCameraExit = true;
    [SerializeField] private float outOfBoundsMargin = 0.05f;

    private Rigidbody2D rb;
    private PlayerController player;
    private Camera mainCamera;
    private bool isRespawning = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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
        if (dieOnCameraExit && !isRespawning)
        {
            CheckCameraBounds();
        }
    }

    private void CheckCameraBounds()
    {
        if (mainCamera == null) return;

        Vector3 viewportPos = mainCamera.WorldToViewportPoint(transform.position);
        float minBound = 0f - outOfBoundsMargin;
        float maxBound = 1f + outOfBoundsMargin;

        if (viewportPos.x < minBound || viewportPos.x > maxBound || 
            viewportPos.y < minBound || viewportPos.y > maxBound)
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
        
        // Now uses the inspector variable instead of a hardcoded value
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