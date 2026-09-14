using UnityEngine;
using System;

public enum LocalGameState
{
    GameOver,
    Playing
}

public class Manager_GameLocal : MonoBehaviour
{
    public static Manager_GameLocal Instance { get; set; }
    [Header("Entity Tracker")]
    public PlayerComponent pia;
    public PlayerComponent pippa;
    public WagonComponent wagon;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        wagon = WagonComponent.Instance;
    }

    public void SetPlayers(PlayerComponent pippaInstance, PlayerComponent piaInstance)
    {
        pippa = pippaInstance;
        pia = piaInstance;
    }

    public void OnCheckEntity()
    {
        if (wagon == null)
        {
            wagon = WagonComponent.Instance;
        }

        if (wagon == null || pippa == null || pia == null)
        {
            Debug.LogWarning("Manager_GameLocal: Gameplay entities have not been registered yet.");
            return;
        }

        if (wagon.isDestroyed)
        {
            SetLocalGameState(LocalGameState.GameOver);
        }

        if (pippa.currentHPStage == PlayerHPStage.KnockedOut &&
            pia.currentHPStage == PlayerHPStage.KnockedOut)
        {
            SetLocalGameState(LocalGameState.GameOver);
        }
    }

    public void SetLocalGameState(LocalGameState localGameState)
    {
        if (Manager_Input.Instance == null)
        {
            Debug.LogWarning("Manager_Input.Instance is null; cannot switch input mode.");
            return;
        }

        switch (localGameState)
        {
            case LocalGameState.GameOver:
                Manager_Input.Instance.SwitchMode(GameState.UI);
                ShowGameOverPanel();
                break;
            case LocalGameState.Playing:
                Manager_Input.Instance.SwitchMode(GameState.Gameplay);
                break;
        }
    }

    public void Quit()
    {
        Application.Quit();
        Debug.Log("Quitting...");
    }

    #region Helper Methods
    private void ShowGameOverPanel()
    {
        Manager_UI.Instance.OnShowPanel("GameOver");
    }
    #endregion
}