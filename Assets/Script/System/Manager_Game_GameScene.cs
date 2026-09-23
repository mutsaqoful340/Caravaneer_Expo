/// <summary>
/// This script is responsible for managing the game scene based on the selected game type.
/// It sets the current game scene in the Manager_Game singleton based on the GameType enum and then commit suicide.
/// </summary>
using UnityEngine;
public enum GameType
{
    UI,
    Gameplay,
    VN
}

public class Manager_Game_GameScene : MonoBehaviour
{
    public static Manager_Game_GameScene Instance {get; set;}
    public GameType currentGameType = GameType.UI;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SetGameType(currentGameType);
    }

    public void SetGameTypeToMainMenu()
    {
        SetGameType(GameType.UI);
    }

    public void SetGameTypeToGameplay()
    {
        SetGameType(GameType.Gameplay);
    }

    public void SetGameTypeToVN()
    {
        SetGameType(GameType.VN);
    }

    public void SetGameType(GameType newGameType)
    {
        if (Manager_Game.Instance == null)
        {
            Debug.LogError("Manager_Game instance is null. Ensure that the Manager_Game script is attached to a GameObject in the scene.");
            return;
        }

        currentGameType = newGameType;

        switch (newGameType)
        {
            case GameType.UI:
                Manager_Game.Instance.SetScene(GameScene.UIScene);
                Manager_Game.Instance.SetState(GameState.UI);
                break;
            case GameType.Gameplay:
                Manager_Game.Instance.SetScene(GameScene.GameplayScene);
                Manager_Game.Instance.SetState(GameState.Gameplay);
                break;
            case GameType.VN:
                Manager_Game.Instance.SetScene(GameScene.VNScene);
                Manager_Game.Instance.SetState(GameState.VN);
                break;
            default:
                Debug.LogError($"Unsupported game type: {newGameType}");
                return;
        }

        // Destroy(gameObject);
    }
}