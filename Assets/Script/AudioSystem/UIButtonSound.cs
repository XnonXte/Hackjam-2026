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

    private void Awake()
    {
        button = GetComponent<Button>();

        // Mendaftarkan fungsi OnClickSound ke event klik tombol secara otomatis
        button.onClick.AddListener(OnClickSound);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverSFX != null && AudioManager.Instance != null && button.interactable)
        {
            AudioManager.Instance.PlayUISFX(hoverSFX);
        }
    }

    private void OnClickSound()
    {
        if (clickSFX != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUISFX(clickSFX);
        }
    }
}