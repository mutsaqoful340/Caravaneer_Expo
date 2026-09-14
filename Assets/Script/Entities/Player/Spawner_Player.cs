using UnityEngine;
using UnityEngine.InputSystem;

public class Spawner_Player : MonoBehaviour
{
    public static Spawner_Player Instance { get; private set; }
    [Header("Player Settings")]
    public int playerMercHPStart = 3; // Default/pre-buff starting HP for Mercenary player
    public int playerMechHPStart = 3; // Default/pre-buff starting HP for Mechanic player
    [SerializeField] private GameObject playerPrefab_1;
    [SerializeField] private GameObject playerPrefab_2;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Make sure a keyboard is plugged in
        if (Keyboard.current == null) return;
    }

    public void SpawnPlayer(Transform mechanicSpawnPoint, Transform mercenarySpawnPoint, GameObject folder)
    {
        if (folder == null)
        {
            Debug.LogWarning("Spawner_Player: 'folder' is not assigned. Players will be spawned at root level.");
        }

        // Instantiate Player 1 using the P1 control scheme
        var p1 = PlayerInput.Instantiate(
            playerPrefab_1,
            playerIndex: 0,
            controlScheme: "P1",
            pairWithDevice: Keyboard.current
        );
        var p1Component = p1.GetComponent<PlayerComponent>();
        if (p1Component != null)
        {
            p1Component.prevParent = folder != null ? folder : (p1.transform.parent != null ? p1.transform.parent.gameObject : p1.gameObject);
            p1Component.InitializeStartingHP(playerMechHPStart);
        }
        if (folder != null) p1.transform.SetParent(folder.transform, true);
        p1.transform.position = mechanicSpawnPoint.position;
        p1.gameObject.name = "Player_Mechanic";

        // Instantiate Player 2 using the P2 control scheme
        var p2 = PlayerInput.Instantiate(
            playerPrefab_2,
            playerIndex: 1,
            controlScheme: "P2",
            pairWithDevice: Keyboard.current
        );
        var p2Component = p2.GetComponent<PlayerComponent>();
        if (p2Component != null)
        {
            p2Component.prevParent = folder != null ? folder : (p2.transform.parent != null ? p2.transform.parent.gameObject : p2.gameObject);
            p2Component.InitializeStartingHP(playerMercHPStart);
        }
        if (folder != null) p2.transform.SetParent(folder.transform, true);
        p2.transform.position = mercenarySpawnPoint.position;
        p2.gameObject.name = "Player_Mercenary";

        Map map = FindAnyObjectByType<Map>();
        if (map != null && p1Component != null && p2Component != null)
        {
            map.SetPlayers(p2Component, p1Component);
        }

        if (p1Component == null || p2Component == null)
        {
            Debug.LogError("Spawner_Player: One or both player prefabs are missing a PlayerComponent.");
            return;
        }

        if (Manager_GameLocal.Instance == null)
        {
            Debug.LogError("Spawner_Player: Manager_GameLocal.Instance is null. If this scene not Main Menu, add Manager_GameLocal to the gameplay scene.");
            return;
        }

        Manager_GameLocal.Instance.SetPlayers(p2Component, p1Component);
    }
}