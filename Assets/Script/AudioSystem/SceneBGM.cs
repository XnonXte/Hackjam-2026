using UnityEngine;

public class SceneBGM : MonoBehaviour
{
    [Header("Background Music")]
    public AudioClip sceneBGM;

    private void Start()
    {
        // Panggil AudioManager untuk memutar lagu ini saat scene dimulai
        if (AudioManager.Instance != null && sceneBGM != null)
        {
            AudioManager.Instance.PlayBGM(sceneBGM);
        }
        else
        {
            Debug.LogWarning("AudioManager tidak ditemukan atau BGM kosong di scene ini.");
        }
    }
}