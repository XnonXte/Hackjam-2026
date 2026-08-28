using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Sandwich : MonoBehaviour
{
    [Header("Level Settings")]
    [Tooltip("Isi dengan angka level saat ini (misal: 1, atau 0 untuk tutorial)")]
    public int currentLevelID;

    [Header("Floating Animation")]
    [SerializeField] private float floatAmplitude = 0.25f;
    [SerializeField] private float floatFrequency = 2f;

    [Header("SFX")]
    [SerializeField] private AudioClip collectSound;

    [Header("Visual Effects")]
    [SerializeField] private GameObject pickupEffectPrefab;

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
        if (isCompleted) return;

        float newY = startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
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

            // --- SPAWN ANIMASI DI SINI ---
            if (pickupEffectPrefab != null)
            {
                Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
            }

            // --- LOGIKA TUTORIAL BARU ---
            // Cek apakah ini level tutorial (misalnya kamu set ID level tutorial menjadi 0)
            if (currentLevelID == 0)
            {
                DataManager.CompleteTutorial();
            }

            DataManager.SaveLevelProgress(currentLevelID, true, finishTime, cokesObtained);

            Debug.Log($"Objective reached! Level {currentLevelID} Complete!");

            GameSessionManager.Instance.ResetSession();
            gameObject.SetActive(false);
            GameManager.Instance.LevelComplete();
        }
    }
}