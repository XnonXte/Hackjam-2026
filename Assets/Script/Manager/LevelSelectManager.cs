using TMPro;
using UnityEngine;

public class LevelSelectionManager : MonoBehaviour
{
    public static LevelSelectionManager Instance;

    [Header("Referensi")]
    public LevelButtonUI[] allLevelButtons; // Drag semua tombol level ke sini
    public LevelModalUI modalUI;            // Drag panel modal ke sini
    public TextMeshProUGUI cokeCounter;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        // 1. Pastikan modal tertutup saat menu baru dibuka
        modalUI.CloseModal();

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

    // Fungsi jembatan: Tombol Level -> Manager -> Panel Modal
    public void OpenModal(int levelID, LevelData data)
    {
        modalUI.ShowModal(levelID, data);
    }
}