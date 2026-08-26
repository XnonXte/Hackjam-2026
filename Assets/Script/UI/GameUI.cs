using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    public static GameUI Instance; // Tambahkan Singleton

    [Header("UI Panels")]
    public GameObject pausePanel;
    public GameObject gamePanel;
    public GameObject levelCompletePanel;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        pausePanel.SetActive(false);
        gamePanel.SetActive(true);
        levelCompletePanel.SetActive(false);
    }

    // --- FUNGSI UPDATE TAMPILAN (Dipanggil oleh GameManager) ---
    public void ShowPauseMenu(bool show)
    {
        pausePanel.SetActive(show);
        gamePanel.SetActive(!show);
    }

    public void ShowLevelComplete()
    {
        levelCompletePanel.SetActive(true);
        gamePanel.SetActive(false);
        pausePanel.SetActive(false); // Pastikan pause tertutup
    }

    // --- HANDLE BUTTON CLICKS ---
    public void OnClickPause()
    {
        GameManager.Instance.TogglePause();
    }

    public void OnClickExit()
    {
        GameManager.Instance.ExitToMenu();
    }

    public void OnClickRestart()
    {
        GameManager.Instance.RestartLevel();
    }

    public void OnClickNextLevel()
    {
        GameManager.Instance.NextLevel();
    }
}