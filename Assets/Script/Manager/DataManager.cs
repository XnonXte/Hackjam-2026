using System.Collections.Generic;
using System.IO;
using UnityEngine;

// --- DATA MODELS ---
[System.Serializable]
public class CokeStatus
{
    public string cokeID;
    public bool isCollected;
}

[System.Serializable]
public class LevelData
{
    public int levelID;
    public List<CokeStatus> cokeCollected = new List<CokeStatus>();
    public bool isUnlocked;
    public bool isCompleted;
    public float bestTime;
}

[System.Serializable]
public class GameData
{
    public List<LevelData> levels = new List<LevelData>();
    // --- FITUR BARU: Status penyelesaian tutorial ---
    public bool hasCompletedTutorial = false;
}

// --- DATA MANAGER MAIN CLASS ---
public static class DataManager
{
    private static GameData currentData = null;

    private static string GetSavePath()
    {
        string path = Application.persistentDataPath + "/savedata.json";
        return path;
    }

    public static void LoadData()
    {
        if (File.Exists(GetSavePath()))
        {
            string json = File.ReadAllText(GetSavePath());
            currentData = JsonUtility.FromJson<GameData>(json);
        }
        else
        {
            currentData = new GameData();
        }
    }

    public static GameData GetGameData()
    {
        if (currentData == null) LoadData();
        return currentData;
    }

    // --- FITUR BARU: CEK STATUS TUTORIAL ---
    public static bool HasCompletedTutorial()
    {
        if (currentData == null) LoadData();
        return currentData.hasCompletedTutorial;
    }

    // --- FITUR BARU: SIMPAN STATUS TUTORIAL ---
    public static void CompleteTutorial()
    {
        if (currentData == null) LoadData();

        currentData.hasCompletedTutorial = true;

        string finalJson = JsonUtility.ToJson(currentData, true);
        File.WriteAllText(GetSavePath(), finalJson);
    }

    // --- MENGAMBIL STATUS COKE PER LEVEL ---
    public static bool IsCokeAlreadyCollected(int levelID, string cokeID)
    {
        if (currentData == null) LoadData();
        LevelData lvl = currentData.levels.Find(l => l.levelID == levelID);

        if (lvl != null)
        {
            return lvl.cokeCollected.Exists(c => c.cokeID == cokeID && c.isCollected);
        }
        return false;
    }

    public static int GetTotalCollectedCokes()
    {
        if (currentData == null) LoadData();

        int totalCokes = 0;
        foreach (LevelData level in currentData.levels)
        {
            foreach (CokeStatus coke in level.cokeCollected)
            {
                if (coke.isCollected) totalCokes++;
            }
        }
        return totalCokes;
    }

    public static void SaveLevelProgress(int levelID, bool isCompleted, float finishTime, List<string> sessionCokes)
    {
        if (currentData == null) LoadData();

        LevelData levelData = currentData.levels.Find(lvl => lvl.levelID == levelID);

        if (levelData == null)
        {
            levelData = new LevelData { levelID = levelID };
            levelData.isUnlocked = true;
            currentData.levels.Add(levelData);
        }

        if (isCompleted) levelData.isCompleted = true;

        if (levelData.bestTime == 0 || finishTime < levelData.bestTime)
        {
            levelData.bestTime = finishTime;
        }

        foreach (string cokeID in sessionCokes)
        {
            CokeStatus existingCoke = levelData.cokeCollected.Find(c => c.cokeID == cokeID);
            if (existingCoke == null)
            {
                levelData.cokeCollected.Add(new CokeStatus { cokeID = cokeID, isCollected = true });
            }
        }

        int nextLevelID = levelID + 1;
        LevelData nextLevel = currentData.levels.Find(lvl => lvl.levelID == nextLevelID);

        if (nextLevel == null)
        {
            nextLevel = new LevelData { levelID = nextLevelID };
            nextLevel.isUnlocked = true;
            currentData.levels.Add(nextLevel);
        }
        else
        {
            nextLevel.isUnlocked = true;
        }

        string finalJson = JsonUtility.ToJson(currentData, true);
        File.WriteAllText(GetSavePath(), finalJson);
    }

    public static void ResetData()
    {
        string path = GetSavePath();
        if (File.Exists(path)) File.Delete(path);
        currentData = new GameData();
        Debug.LogWarning("⚠️ Save Data telah di-reset dan dihapus!");
    }
}