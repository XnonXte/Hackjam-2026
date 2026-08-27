using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelModalUI : MonoBehaviour
{
    [Header("Referensi UI")]
    public GameObject modalPanel;
    public TextMeshProUGUI levelTitleText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI timeText;

    private int selectedLevelID;

    public void ShowModal(int levelID, LevelData data)
    {
        selectedLevelID = levelID;
        modalPanel.SetActive(true);

        levelTitleText.text = "LEVEL " + levelID;

        if (data != null && data.isCompleted)
        {
            statusText.text = "Status: Selesai";
            timeText.text = "Waktu: " + data.bestTime.ToString("F2") + "s";
        }
        else
        {
            statusText.text = "Status: Belum Selesai";
            timeText.text = "Waktu: --";
        }
    }

    public void CloseModal()
    {
        modalPanel.SetActive(false);
    }

    public void StartGame()
    {
        GameManager.Instance.levelID = selectedLevelID;
        Time.timeScale = 1f;
        SceneManager.LoadScene("Level " + selectedLevelID);
    }
}