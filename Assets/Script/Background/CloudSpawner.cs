using UnityEngine;

public class CloudSpawner : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 2.0f;
    [Tooltip("How many clouds to spawn in the chain")]
    public int numberOfClouds = 3; 

    [Header("Cloud Prefab")]
    [Tooltip("Assign your cloud prefab here (must have a SpriteRenderer)")]
    public GameObject cloudPrefab; 

    private Transform[] clouds;
    private float cloudWidth;

    void Start()
    {
        if (cloudPrefab == null)
        {
            Debug.LogError("Please assign a Cloud Prefab in the inspector.");
            return;
        }

        SpriteRenderer sr = cloudPrefab.GetComponent<SpriteRenderer>();
        if (sr == null) return;
        
        // Exact width, no overlap hack needed
        cloudWidth = sr.bounds.size.x;
        clouds = new Transform[numberOfClouds];

        // Instantiate and strictly space them by exact width multiples
        for (int i = 0; i < numberOfClouds; i++)
        {
            GameObject newCloud = Instantiate(cloudPrefab, transform);
            
            float startX = -cloudWidth * i;
            newCloud.transform.position = new Vector3(startX, transform.position.y, transform.position.z);
            
            clouds[i] = newCloud.transform;
        }
    }

    void Update()
    {
        if (clouds == null || clouds.Length == 0) return;

        foreach (Transform cloud in clouds)
        {
            // Move the cloud from left to right
            cloud.Translate(Vector3.right * speed * Time.deltaTime);

            // Once the cloud moves far enough to the right
            if (cloud.position.x >= cloudWidth * 1.5f)
            {
                // MATHEMATICAL WRAP: Move it back exactly by the total width of all clouds combined.
                // This perfectly preserves any sub-pixel floating point data, completely eliminating gaps!
                cloud.position -= new Vector3(cloudWidth * numberOfClouds, 0, 0);
            }
        }
    }
}