using System.Collections.Generic;
using UnityEngine;
// using UnityEngine.SceneManagement; // Buka comment ini nanti kalau mau pindah scene

[RequireComponent(typeof(Collider2D))]
public class Sandwich : MonoBehaviour
{
    [Header("Level Settings")]
    [Tooltip("Isi dengan angka level saat ini (misal: 1)")]
    public int currentLevelID;

    [Header("Floating Animation")]
    [SerializeField] private float floatAmplitude = 0.25f;
    [SerializeField] private float floatFrequency = 2f;

    [Header("SFX")]
    [SerializeField] private AudioClip collectSound;

    private Vector3 startPos;
    private bool isCompleted = false;

    private void Start()
    {
        currentLevelID = GameManager.Instance.levelID;
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

            if (collectSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFXAtPosition(collectSound, transform.position);
            }

            float finishTime = Time.timeSinceLevelLoad;

            List<string> cokesObtained = GameSessionManager.Instance.collectedCokesInSession;
            DataManager.SaveLevelProgress(currentLevelID, true, finishTime, cokesObtained);

            // Log untuk keperluan Debugging
            Debug.Log($"Objective reached! Level {currentLevelID} Complete!");
            Debug.Log($"Best Time Baru: {finishTime.ToString("F2")} detik.");
            Debug.Log($"Coke Diselamatkan: {string.Join(", ", cokesObtained)}");

            GameSessionManager.Instance.ResetSession();
            gameObject.SetActive(false);
            GameManager.Instance.LevelComplete();
        }
    }
}