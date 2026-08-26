using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game States")]
    public bool isGameOver = false;
    public bool isPaused = false;
    public int levelID;

    [Header("References")]
    public InputActionReference pauseAction;

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

    private void Update()
    {
        if (pauseAction.action.WasPressedThisFrame() && !isGameOver)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        // Beritahu UI untuk mengubah tampilan
        if (GameUI.Instance != null)
        {
            GameUI.Instance.ShowPauseMenu(isPaused);
        }
    }

    public void LevelComplete()
    {
        isGameOver = true;

        // Beritahu UI untuk menampilkan panel menang
        if (GameUI.Instance != null)
        {
            GameUI.Instance.ShowLevelComplete();
        }
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        if (GameSessionManager.Instance != null) GameSessionManager.Instance.ResetSession();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ExitToMenu()
    {
        Time.timeScale = 1f;
        if (GameSessionManager.Instance != null) GameSessionManager.Instance.ResetSession();
        SceneManager.LoadScene("MainMenu");
    }

    public void NextLevel()
    {
        Time.timeScale = 1f;
        if (GameSessionManager.Instance != null) GameSessionManager.Instance.ResetSession();

        int nextLevelTarget = levelID + 1;
        string nextSceneName = "Level_" + nextLevelTarget;

        if (Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            isGameOver = false;
            isPaused = false;
            levelID = nextLevelTarget;
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.Log("Ini level terakhir! Kembali ke menu pilihan level.");
            SceneManager.LoadScene("MainMenu");
        }
    }
}