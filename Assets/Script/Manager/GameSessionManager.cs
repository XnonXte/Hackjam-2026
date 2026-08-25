using System.Collections.Generic;
using UnityEngine;

public class GameSessionManager : MonoBehaviour
{
    public static GameSessionManager Instance;

    public List<string> collectedCokesInSession = new List<string>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }


    public void AddCoke(string cokeID)
    {
        if (!collectedCokesInSession.Contains(cokeID))
        {
            collectedCokesInSession.Add(cokeID);
        }
    }

    [ContextMenu("Reset Session")]
    public void ResetSession()
    {
        collectedCokesInSession.Clear();
    }
}