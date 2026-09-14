using UnityEngine;
using TMPro;

public class Player_LocalInvenvory : MonoBehaviour
{
    public static Player_LocalInvenvory Instance { get; private set; }
    public TextMeshProUGUI textCoins;
    public TextMeshProUGUI textRepairMaterials;

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
        if (PlayerInventory.Instance == null)
        {
            Debug.LogError("PlayerInventory instance is null. Cannot proceed with initializing the local inventory.");
            return;
        }

        // Initialize the local inventory with the player's current coins and repair materials
        UpdateLocalInventory();
    }

    public void UpdateLocalInventory()
    {
        textCoins.text = PlayerInventory.Instance.coins.ToString();
        textRepairMaterials.text = PlayerInventory.Instance.repairMaterials.ToString();
    }
}
