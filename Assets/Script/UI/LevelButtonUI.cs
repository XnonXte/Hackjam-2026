using UnityEngine;
using UnityEngine.UI; // Wajib untuk mengakses komponen UI

public class LevelButtonUI : MonoBehaviour
{
    [Header("Identitas")]
    public int levelID;

    [Header("Komponen UI")]
    public Image buttonImage;
    public Button buttonComponent;

    [Header("Sprites State")]
    public Sprite lockedSprite;
    public Sprite unlockedSprite;
    public Sprite completedSprite;

    // Dipanggil oleh Manager saat pertama kali scene dibuka
    public void Setup(LevelData data)
    {
        // Level 1 selalu otomatis terbuka, atau level lain yang isUnlocked = true
        bool isUnlocked = (data != null && data.isUnlocked) || levelID == 1;
        bool isCompleted = data != null && data.isCompleted;

        // Kunci interaksi tombol jika belum unlocked
        buttonComponent.interactable = isUnlocked;

        // Logika pergantian Sprite
        if (isCompleted)
        {
            buttonImage.sprite = completedSprite;
        }
        else if (isUnlocked)
        {
            buttonImage.sprite = unlockedSprite;
        }
        else
        {
            buttonImage.sprite = lockedSprite;
        }
    }

    // Sambungkan fungsi ini ke event OnClick() di komponen Button Unity
    public void OnClickStartGame()
    {
        GameManager.Instance.levelID = levelID;
        LevelSelectionManager.Instance.StartGame(levelID);
    }
}