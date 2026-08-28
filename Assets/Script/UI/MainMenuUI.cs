using UnityEngine;
using UnityEngine.SceneManagement; // Tambahkan ini untuk memuat scene

public class MainMenuUI : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject levelSelectPanel;

    [Header("Tutorial Settings")]
    [SerializeField] private string tutorialSceneName = "Tutorial"; // Sesuaikan dengan nama scenemu

    private void Start()
    {
        ShowMainMenu();
    }

    // --- FUNGSI BARU UNTUK TOMBOL "START GAME" ---
    public void OnStartGameClicked()
    {
        // Mengecek data JSON
        if (!DataManager.HasCompletedTutorial())
        {
            // Jika belum tamat tutorial, langsung lempar ke scene tutorial
            GameManager.Instance.levelID = 0;
            SceneManager.LoadScene(tutorialSceneName);
        }
        else
        {
            // Jika sudah tamat, buka panel Level Select seperti biasa
            ShowLevelSelect();
        }
    }

    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        levelSelectPanel.SetActive(false);
    }

    public void ShowSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
        creditsPanel.SetActive(false);
        levelSelectPanel.SetActive(false);
    }

    public void ShowCredits()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(true);
        levelSelectPanel.SetActive(false);
    }

    public void ShowLevelSelect()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        levelSelectPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}