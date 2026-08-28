using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ResetData : MonoBehaviour
{

    public void ResetDataGame()
    {
        DataManager.ResetData();
        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.ResetSession();
        }

        // 3. Reload scene saat ini agar UI mereset tampilannya!
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        Debug.Log("Game di-reset dan Scene di-reload!");
    }

}