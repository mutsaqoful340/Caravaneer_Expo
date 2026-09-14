using UnityEngine;

public class Spawner_TriggerLocalEntities : MonoBehaviour
{
    public static Spawner_TriggerLocalEntities Instance {set; get;}
    public bool isMainMenu = false;
    public Transform spawnPoint_Mechanics;
    public Transform spawnPoint_Mercenary;
    public WagonComponent wagonObject;
    public GameObject folder;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (!isMainMenu)
        {
            Spawner_Wagon.Instance.UpdateWagonStat();
        }
        Spawner_Player.Instance.SpawnPlayer(spawnPoint_Mechanics, spawnPoint_Mercenary, folder);
    }
}