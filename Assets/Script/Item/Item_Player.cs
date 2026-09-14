using UnityEngine;
using UnityEngine.UI;

public enum HPType
{
    Pia,
    Pippa
}

public class Item_Player : MonoBehaviour
{
    public HPType hPType;
    public CanvasGroup parentCanvas;
    public Selectable fallbackSelectable;
    public ItemData itemData;
    public int itemModifierValue; // This can be used to modify the item's effect, e.g., amount of HP restored

    private bool isPurchased;

    public void OnClickItem()
    {
        if (itemData == null || UI_UnivConfirmPanel.Instance == null)
        {
            Debug.LogError("Item_Player: Item data or confirmation panel is not available.");
            return;
        }

        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<CanvasGroup>();
        }

        UI_UnivConfirmPanel.Instance.OnShow(
            () => UseItem(),
            () => Debug.Log("Item purchase canceled."),
            parentCanvas,
            fallbackSelectable
        );
    }

    private void UseItem()
    {
        if (isPurchased)
        {
            return;
        }

        if (PlayerInventory.Instance == null || Spawner_Player.Instance == null ||
            itemData == null || string.IsNullOrWhiteSpace(itemData.itemID))
        {
            Debug.LogError("Item_Player: A required purchase reference or item ID is missing.");
            return;
        }

        if (PlayerInventory.Instance.HasBoughtItem(itemData.itemID))
        {
            isPurchased = true;
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        if (hPType != HPType.Pia && hPType != HPType.Pippa)
        {
            Debug.LogWarning($"Item_Player: HP type {hPType} is not implemented.");
            return;
        }

        isPurchased = true;
        if (!PlayerInventory.Instance.TrySpendCoins(itemData.itemPrice))
        {
            isPurchased = false;
            Debug.LogWarning("Not enough coins to buy this item.");
            return;
        }

        if (hPType == HPType.Pia)
        {
            Spawner_Player.Instance.playerMechHPStart += itemModifierValue;
        }
        else
        {
            Spawner_Player.Instance.playerMercHPStart += itemModifierValue;
        }

        PlayerInventory.Instance.MarkItemAsBought(itemData.itemID);
        Debug.Log($"Purchased {itemData.itemID} {itemData.itemName} to increase player HP by {itemModifierValue}.");
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}