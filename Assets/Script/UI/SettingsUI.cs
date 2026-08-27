using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("Slider References")]
    public Slider bgmSlider;
    public Slider sfxSlider;

    private void Start()
    {
        // 1. Ambil data volume yang tersimpan (default 1 jika belum pernah disetting)
        float savedBGM = PlayerPrefs.GetFloat("SavedBGMVolume", 1f);
        float savedSFX = PlayerPrefs.GetFloat("SavedSFXVolume", 1f);

        // 2. Set posisi slider sesuai data yang tersimpan
        if (bgmSlider != null) bgmSlider.value = savedBGM;
        if (sfxSlider != null) sfxSlider.value = savedSFX;

        // 3. Terapkan volume ke AudioManager saat baru mulai
        UpdateBGMVolume(savedBGM);
        UpdateSFXVolume(savedSFX);

        // 4. Tambahkan listener agar fungsi dipanggil otomatis saat slider digeser
        if (bgmSlider != null)
            bgmSlider.onValueChanged.AddListener(UpdateBGMVolume);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(UpdateSFXVolume);
    }

    public void UpdateBGMVolume(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBGMVolume(value);
        }
        // Simpan otomatis setiap kali digeser
        PlayerPrefs.SetFloat("SavedBGMVolume", value);
    }

    public void UpdateSFXVolume(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value);
        }
        PlayerPrefs.SetFloat("SavedSFXVolume", value);
    }
}