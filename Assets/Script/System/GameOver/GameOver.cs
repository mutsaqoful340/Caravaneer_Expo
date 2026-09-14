using UnityEngine;

public class GameOver : MonoBehaviour
{
    public void OnRetry(string sceneName)
    {
        SceneLoader.Instance.LoadScene(sceneName);
    }

    public void OnReturnToMainMenu(string sceneName)
    {
        SceneLoader.Instance.LoadScene(sceneName);
        Manager_Game.Instance.SetState(GameState.Gameplay);
    }
}