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

            // --- LOGIKA SAVE SYSTEM & END LEVEL ---

            // 1. Ambil waktu penyelesaian (dari scene mulai sampai menyentuh objective)
            float finishTime = Time.timeSinceLevelLoad;

            // 2. Ambil list Coke dari sesi sementara ini
            List<string> cokesObtained = GameSessionManager.Instance.collectedCokesInSession;

            // 3. Simpan progress ke JSON melalui DataManager
            DataManager.SaveLevelProgress(currentLevelID, true, finishTime, cokesObtained);

            // Log untuk keperluan Debugging
            Debug.Log($"Objective reached! Level {currentLevelID} Complete!");
            Debug.Log($"Best Time Baru: {finishTime.ToString("F2")} detik.");
            Debug.Log($"Coke Diselamatkan: {string.Join(", ", cokesObtained)}");

            // 4. Bersihkan memori sesi sementara agar siap untuk level selanjutnya
            GameSessionManager.Instance.ResetSession();

            gameObject.SetActive(false); // Optional: sembunyikan sandwich setelah level selesai

            // 5. Load Scene berikutnya / kembali ke Level Selection
            // SceneManager.LoadScene("LevelSelection");
        }
    }
}