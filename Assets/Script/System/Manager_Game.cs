using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public enum GameState
{
    UI,
    Gameplay,
    VN
}

public enum GameScene
{
    MainMenuScene,
    GameplayScene,
    VNScene
}

public class Manager_Game : MonoBehaviour
{
    public static Manager_Game Instance { get; private set; }
    [Tooltip("Manages player input modes.")]
    public GameState currentGameState = GameState.Gameplay;
    [Tooltip("Manages the current game scene.")]
    public GameScene currentGameScene = GameScene.MainMenuScene;

    private void Awake()
    {
        // Cursor.lockState = CursorLockMode.Confined;
        // Cursor.visible = false;
        currentGameState = GameState.Gameplay;
        // currentGameScene = GameScene.MainMenu;

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        QualitySettings.vSyncCount = 0; // Disable VSync
        Application.targetFrameRate = 60;
    }

    public void SetState(GameState newState)
    {
        if (currentGameState == newState)
            return;

        currentGameState = newState;
        OnGameStateChanged(newState);
    }

    private void OnGameStateChanged(GameState newState)
    {
        if (Manager_Input.Instance == null)
        {
            Debug.LogWarning("Manager_Input.Instance is null; cannot switch input mode.");
            return;
        }

        switch (newState)
        {
            case GameState.UI:
                Manager_Input.Instance.SwitchMode(GameState.UI);
                break;
            case GameState.Gameplay:
                Manager_Input.Instance.SwitchMode(GameState.Gameplay);
                break;
            case GameState.VN:
                Manager_Input.Instance.SwitchMode(GameState.VN);
                break;
        }
    }

    public void SetScene(GameScene newScene)
    {
        if (currentGameScene == newScene)
            return;

        currentGameScene = newScene;
        OnGameSceneChanged(newScene);
    }

    private void OnGameSceneChanged(GameScene newScene)
    {
        switch (newScene)
        {
            case GameScene.MainMenuScene:
                // Handle main menu scene logic
                break;
            case GameScene.GameplayScene:
                // Handle gameplay scene logic
                break;
            case GameScene.VNScene:
                // Handle VN scene logic
                break;
        }
    }

    public void OnQuit()
    {
        Application.Quit();
        Debug.Log("Quitting..");
    }
}
