/// <summary>
/// This script is responsible for managing the game scene based on the selected game type.
/// It sets the current game scene in the Manager_Game singleton based on the GameType enum and then commit suicide.
/// </summary>
using UnityEngine;
public enum GameType
{
    MainMenu,
    Gameplay,
    VN
}

public class Manager_Game_GameScene : MonoBehaviour
{
    public static Manager_Game_GameScene Instance {get; set;}
    public GameType currentGameType = GameType.MainMenu;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SetGameType(currentGameType);
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
            case GameType.MainMenu:
                Manager_Game.Instance.SetScene(GameScene.MainMenuScene);
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