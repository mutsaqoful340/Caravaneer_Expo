using UnityEngine;
using TMPro;
public class GameWin : MonoBehaviour
{
    [Tooltip("Reward amount given to the player upon winning the game.")]
    public int rewardAmount;
    public TextMeshProUGUI playTimeDuration;
    
    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Wagon"))
        {
            RewardPlayer();
            OnGameWin("GameWin");
        }
    }

    public void OnGameWin(string panelName)
    {
        Manager_UI.Instance.OnShowPanel(panelName);
        Manager_Game.Instance.SetState(GameState.UI);
    }

    private void RewardPlayer()
    {
        PlayerInventory.Instance.AddCoins(rewardAmount);
        if (Time_Stopwatch.Instance != null)
        {
            Time_Stopwatch.Instance?.StopStopwatch();
            playTimeDuration.text = Time_Stopwatch.Instance?.timerText?.text;
        }
    }
}