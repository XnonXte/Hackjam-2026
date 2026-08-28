using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectionManager : MonoBehaviour
{
    public static LevelSelectionManager Instance;

    [Header("Referensi")]
    public LevelButtonUI[] allLevelButtons;
    public TextMeshProUGUI cokeCounter;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        // 2. Ambil seluruh data permanen dari DataManager
        GameData data = DataManager.GetGameData();

        // 3. Update sprite dan data setiap tombol level
        foreach (LevelButtonUI btn in allLevelButtons)
        {
            // Cari data yang ID-nya cocok dengan ID tombol
            LevelData lvlData = data.levels.Find(l => l.levelID == btn.levelID);

            // Perintahkan tombol untuk mengatur sprite-nya
            btn.Setup(lvlData);
        }

        cokeCounter.text = DataManager.GetTotalCollectedCokes().ToString();
    }

    public void StartGame(int levelID)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Level " + levelID);
    }
}