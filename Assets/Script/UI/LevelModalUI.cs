using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelModalUI : MonoBehaviour
{
    [Header("Referensi UI")]
    public GameObject modalPanel;
    public TextMeshProUGUI levelTitleText; // Pakai TextMeshProUGUI jika pakai TMP
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI timeText;

    // [Header("Collectible Icons (Coke)")]
    // public Image[] cokeIcons;
    // public Color collectedColor = Color.white; // Nyala
    // public Color missingColor = new Color(1, 1, 1, 0.3f); // Transparan/Gelap

    private int selectedLevelID;

    // Fungsi ini dipanggil oleh Manager saat tombol level diklik
    public void ShowModal(int levelID, LevelData data)
    {
        selectedLevelID = levelID;
        modalPanel.SetActive(true); // Tampilkan panel

        levelTitleText.text = "LEVEL " + levelID;

        // Cek data level
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

        // Update indikator Coke
        // for (int i = 0; i < cokeIcons.Length; i++)
        // {
        //     // Cek apakah data coke ada dan sudah diambil
        //     if (data != null && i < data.cokeCollected.Count && data.cokeCollected[i].isCollected)
        //     {
        //         cokeIcons[i].color = collectedColor;
        //     }
        //     else
        //     {
        //         cokeIcons[i].color = missingColor;
        //     }
        // }
    }

    // Fungsi untuk tombol "Back"
    public void CloseModal()
    {
        modalPanel.SetActive(false);
    }

    // Fungsi untuk tombol "Start"
    public void StartGame()
    {
        // Pindah ke scene level yang dipilih
        SceneManager.LoadScene("Level_" + selectedLevelID);
    }
}