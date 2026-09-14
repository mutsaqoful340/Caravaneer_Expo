using UnityEngine;

public class Spawner_Wagon : MonoBehaviour
{
    public static Spawner_Wagon Instance { get; private set; }
    [Header("Wagon Settings")]
    public int wagonHPFunctionalStart = 5; // Default/pre-buff starting HP for functional wagons
    public int wagonHPBrokenStart = 6; // Default/pre-buff starting HP for broken wagons
    [SerializeField] private WagonComponent wagon;

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

    public void UpdateWagonStat()
    {
        if (!Spawner_TriggerLocalEntities.Instance.wagonObject)
        {
            Spawner_TriggerLocalEntities.Instance.wagonObject.InitializeStartingHP(wagonHPFunctionalStart, wagonHPBrokenStart);
        }
        else
        {
            Debug.LogWarning("The existing scene wagon is not assigned or available.");
        }
    }
}