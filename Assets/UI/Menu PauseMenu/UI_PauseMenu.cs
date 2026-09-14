using UnityEngine;

public class UI_PauseMenu : MonoBehaviour
{
    public void OnResumeButtonPressed()
    {
        Manager_Game.Instance.SetState(GameState.Gameplay);
        Manager_UI.Instance.OnCloseAllPanels();
    }

    public void OnReturnToMainMenuButtonPressed(string sceneName)
    {
        Manager_Game.Instance.SetState(GameState.Gameplay);
        Manager_UI.Instance.OnCloseAllPanels();
        Debug.Log("Returning to Main Menu...");
        SceneLoader.Instance.LoadScene(sceneName);
    }

    public void OnQuitGameButtonPressed()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }
}