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
    public GameObject tutorialCompletePanel;
    public GameObject abilitySelectionPanel; // Boleh kosong (null) - lihat catatan di Start()

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
        tutorialCompletePanel.SetActive(false);

        // Kalau abilitySelectionPanel gak di-assign, anggap level ini
        // memang tidak punya fase ability-selection sama sekali -
        // langsung perlakukan seperti tutorial (levelID == 0).
        if (abilitySelectionPanel == null)
        {
            gamePanel.SetActive(true);
            isSelectingAbility = false;
        }
        else
        {
            abilitySelectionPanel.SetActive(true);
            isSelectingAbility = true; // Set state awal

            if (GameManager.Instance.levelID == 0)
            {
                gamePanel.SetActive(true);
                abilitySelectionPanel.SetActive(false);
                isSelectingAbility = false;
            }
        }
    }

    // --- FUNGSI UPDATE TAMPILAN (Dipanggil oleh GameManager) ---
    public void ShowPauseMenu(bool show)
    {
        pausePanel.SetActive(show);

        if (!isSelectingAbility)
        {
            gamePanel.SetActive(!show);
        }
        else
        {
            abilitySelectionPanel?.SetActive(!show);
        }

    }

    public void ShowLevelComplete()
    {
        if (GameManager.Instance.levelID == 0)
        {
            tutorialCompletePanel.SetActive(true);
            levelCompletePanel.SetActive(false);
            gamePanel.SetActive(false);
            pausePanel.SetActive(false);
            abilitySelectionPanel?.SetActive(false);
        }
        else
        {
            levelCompletePanel.SetActive(true);
            tutorialCompletePanel.SetActive(false);
            gamePanel.SetActive(false);
            pausePanel.SetActive(false);
            abilitySelectionPanel?.SetActive(false);
        }

    }

    public void ShowGamePanel()
    {
        isSelectingAbility = false;

        levelCompletePanel.SetActive(false);
        tutorialCompletePanel.SetActive(false);
        gamePanel.SetActive(true);
        pausePanel.SetActive(false);
        abilitySelectionPanel?.SetActive(false);
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