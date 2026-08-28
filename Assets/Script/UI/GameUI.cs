using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    public static GameUI Instance;

    [Header("UI Panels")]
    public GameObject pausePanel;
    public GameObject gamePanel;
    public GameObject levelCompletePanel;
    public GameObject SettingsPanel;

    [Header("References")]
    public InputActionReference pauseAction;

    // STATE PENANDA
    public bool isSelectingAbility = true;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Update()
    {
        if (pauseAction.action.WasPressedThisFrame() && !GameManager.Instance.isGameOver)
        {
            GameManager.Instance.TogglePause();
        }
    }

    private void Start()
    {
        pausePanel.SetActive(false);
        gamePanel.SetActive(false);
        levelCompletePanel.SetActive(false);
        SettingsPanel.SetActive(false);
        isSelectingAbility = true; // Set state awal
    }

    // --- FUNGSI UPDATE TAMPILAN (Dipanggil oleh GameManager) ---
    public void ShowPauseMenu(bool show)
    {
        pausePanel.SetActive(show);

        if (!isSelectingAbility)
        {
            gamePanel.SetActive(!show);
        }
    }

    public void ShowLevelComplete()
    {
        levelCompletePanel.SetActive(true);
        gamePanel.SetActive(false);
        pausePanel.SetActive(false);
    }

    public void ShowGamePanel()
    {
        isSelectingAbility = false;

        levelCompletePanel.SetActive(false);
        gamePanel.SetActive(true);
        pausePanel.SetActive(false);
    }

    public void ShowSettingsPanel(bool show)
    {
        SettingsPanel.SetActive(show);

        pausePanel.SetActive(!show);
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