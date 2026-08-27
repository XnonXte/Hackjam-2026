using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))] // Memaksa Unity agar script ini hanya bisa dipasang di objek yang punya Button
public class UIButtonSound : MonoBehaviour, IPointerEnterHandler
{
    [Header("Audio Clips")]
    [Tooltip("Suara saat kursor menyentuh tombol")]
    public AudioClip hoverSFX;

    [Tooltip("Suara saat tombol diklik")]
    public AudioClip clickSFX;

    private Button button;

    // Tambahkan variabel ini di bagian atas class
    private static float lastHoverTime = 0f;
    private float hoverCooldown = 0.05f; // Jeda 0.05 detik

    private void Awake()
    {
        button = GetComponent<Button>();

        // Mendaftarkan fungsi OnClickSound ke event klik tombol secara otomatis
        button.onClick.AddListener(OnClickSound);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Cek apakah jeda waktu dari hover terakhir sudah melewati cooldown
        if (Time.unscaledTime - lastHoverTime < hoverCooldown) return;

        if (hoverSFX != null && AudioManager.Instance != null && button.interactable)
        {
            AudioManager.Instance.PlaySFX(hoverSFX);
            lastHoverTime = Time.unscaledTime; // Catat waktu hover terakhir
        }
    }

    // Terpanggil otomatis saat tombol diklik
    private void OnClickSound()
    {
        if (clickSFX != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clickSFX);
        }
    }
}