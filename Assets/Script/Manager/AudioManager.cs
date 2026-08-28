using UnityEngine;
using UnityEngine.Audio; // Wajib untuk AudioMixer

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Control")]
    public AudioMixer mainMixer;
    public AudioMixerGroup sfxGroup; // Untuk di-assign ke efek suara lokal

    [Header("Audio Sources (Global)")]
    public AudioSource bgmSource;
    public AudioSource sfxSource; // Untuk suara UI atau efek yang tidak butuh posisi

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // =================================================================
    // PEMUTAR SUARA
    // =================================================================

    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource.clip == clip) return; // Jangan ulang BGM yang sama
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }

    // Gunakan ini khusus untuk UI agar suara tidak menumpuk dan bikin limiter jebol
    public void PlayUISFX(AudioClip clip)
    {
        if (clip == null) return;

        // Memakai .Play() akan otomatis mematikan suara SFX sebelumnya yang sedang jalan di channel ini
        sfxSource.clip = clip;
        sfxSource.Play();
    }

    // =================================================================
    // PENGATUR VOLUME (Logarithmic Scale)
    // =================================================================

    // Value dari slider UI harus antara 0.0001f sampai 1f (jangan nol!)
    public void SetBGMVolume(float sliderValue)
    {
        mainMixer.SetFloat("BGMVolume", Mathf.Log10(sliderValue) * 20);
    }

    public void SetSFXVolume(float sliderValue)
    {
        mainMixer.SetFloat("SFXVolume", Mathf.Log10(sliderValue) * 20);
    }
}