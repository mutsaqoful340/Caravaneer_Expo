using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class Item_Tracker : MonoBehaviour
{
    public PlayerInventory playerInventory;
    public GameObject[] items;
    public Selectable fallbackSelectable;

    private CanvasGroup itemCanvas;

    private void Awake()
    {
        itemCanvas = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        if (!playerInventory)
        {
            playerInventory = PlayerInventory.Instance;
        }

        if (!playerInventory)
        {
            Debug.LogError("Item_Tracker: PlayerInventory instance is not available.");
            return;
        }

        if (items == null)
        {
            Debug.LogWarning("Item_Tracker: No item prefabs are assigned.");
            return;
        }

        foreach (GameObject itemPrefab in items)
        {
            if (!itemPrefab)
            {
                Debug.LogWarning("Item_Tracker: An item prefab is not assigned.");
                continue;
            }

            Item_Player playerItemPrefab = itemPrefab.GetComponent<Item_Player>();
            Item_Wagon wagonItemPrefab = itemPrefab.GetComponent<Item_Wagon>();
            ItemData itemData = playerItemPrefab != null ? playerItemPrefab.itemData : wagonItemPrefab?.itemData;

            if (itemData == null || string.IsNullOrWhiteSpace(itemData.itemID))
            {
                Debug.LogWarning($"Item_Tracker: {itemPrefab.name} needs an Item_Player or Item_Wagon with a valid item ID.");
                continue;
            }

            playerInventory.RegisterItem(itemData.itemID);
            if (playerInventory.HasBoughtItem(itemData.itemID))
            {
                continue;
            }

            GameObject itemInstance = Instantiate(itemPrefab, transform, false);
            Item_Player playerItem = itemInstance.GetComponent<Item_Player>();
            Item_Wagon wagonItem = itemInstance.GetComponent<Item_Wagon>();

            if (playerItem != null)
            {
                playerItem.parentCanvas = itemCanvas;
                playerItem.fallbackSelectable = fallbackSelectable;
            }

            if (wagonItem != null)
            {
                wagonItem.parentCanvas = itemCanvas;
                wagonItem.fallbackSelectable = fallbackSelectable;
            }
        }
    }
}