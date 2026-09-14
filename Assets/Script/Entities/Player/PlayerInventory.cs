/// <summary>
/// This script would work as a collective inventory for both players.
/// Will store items, repair materials, money, and other relevant things.
/// </summary>

using UnityEngine;
using System;

[Serializable]
public class TrackedItem
{
    public string itemID;
    public bool hasBought;
}
public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }
    [Header("Inventory Data")]
    public int coins;
    public int repairMaterials;

    [Header("HUD References")]

    [Header("Other Inventory References")]
    public Animator animator;

    [Header("Store Item")]
    public TrackedItem[] trackedItems = Array.Empty<TrackedItem>();

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
        UpdateHUD();
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        UpdateHUD();
    }

    public void AddRepairMaterials(int amount)
    {
        repairMaterials += amount;
        UpdateHUD();
    }
    
    public bool TrySpendRepairMaterials(int amount)
    {
        if (amount <= 0 || repairMaterials < amount)
        {
            return false;
        }
        
        repairMaterials -= amount;
        UpdateHUD();
        return true;
    }

    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0 || coins < amount)
        {
            return false;
        }
        
        coins -= amount;
        UpdateHUD();
        return true;
    }

    public void RegisterItem(string itemID)
    {
        if (string.IsNullOrWhiteSpace(itemID) || FindTrackedItem(itemID) != null)
        {
            return;
        }

        trackedItems ??= Array.Empty<TrackedItem>();
        int newIndex = trackedItems.Length;
        Array.Resize(ref trackedItems, newIndex + 1);
        trackedItems[newIndex] = new TrackedItem
        {
            itemID = itemID,
            hasBought = false
        };
    }

    public bool HasBoughtItem(string itemID)
    {
        TrackedItem trackedItem = FindTrackedItem(itemID);
        return trackedItem != null && trackedItem.hasBought;
    }

    public void MarkItemAsBought(string itemID)
    {
        RegisterItem(itemID);

        TrackedItem trackedItem = FindTrackedItem(itemID);
        if (trackedItem != null)
        {
            trackedItem.hasBought = true;
        }
    }

    private TrackedItem FindTrackedItem(string itemID)
    {
        if (trackedItems == null || string.IsNullOrWhiteSpace(itemID))
        {
            return null;
        }

        foreach (TrackedItem trackedItem in trackedItems)
        {
            if (trackedItem != null && trackedItem.itemID == itemID)
            {
                return trackedItem;
            }
        }

        return null;
    }

    private void UpdateHUD()
    {
        if (Player_LocalInvenvory.Instance == null)
        {
            Debug.LogWarning("PlayerInventory: HUD references are not assigned.");
            return;
        }
        
        Player_LocalInvenvory.Instance.UpdateLocalInventory();
    }
}