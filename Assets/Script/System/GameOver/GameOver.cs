using TMPro;
using UnityEngine;

public class GameOver : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    void OnEnable()
    {
        Time_Stopwatch.Instance?.StopStopwatch();
        timerText.text = Time_Stopwatch.Instance?.timerText?.text;
    }
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