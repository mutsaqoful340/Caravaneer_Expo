using UnityEngine;
using UnityEngine.UI;

public enum ItemTypeWagon
{
    HPFunctional,
    HPBroken
}

public class Item_Wagon : MonoBehaviour
{
    public ItemTypeWagon itemType;
    public CanvasGroup parentCanvas;
    public Selectable fallbackSelectable;
    public ItemData itemData;
    public int itemModifierValue; // This can be used to modify the item's effect, e.g., amount of HP restored

    private bool isPurchased;

    public void OnClickItem()
    {
        if (itemData == null || UI_UnivConfirmPanel.Instance == null)
        {
            Debug.LogError("Item_Wagon: Item data or confirmation panel is not available.");
            return;
        }

        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<CanvasGroup>();
        }

        UI_UnivConfirmPanel.Instance.OnShow(
            () => BuyItem(),
            () => Debug.Log("Item purchase canceled."),
            parentCanvas,
            fallbackSelectable
        );
    }

    private void BuyItem()
    {
        if (isPurchased)
        {
            return;
        }

        if (PlayerInventory.Instance == null || Spawner_Wagon.Instance == null ||
            itemData == null || string.IsNullOrWhiteSpace(itemData.itemID))
        {
            Debug.LogError("Item_Wagon: A required purchase reference or item ID is missing.");
            return;
        }

        if (PlayerInventory.Instance.HasBoughtItem(itemData.itemID))
        {
            isPurchased = true;
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        if (itemType != ItemTypeWagon.HPFunctional && itemType != ItemTypeWagon.HPBroken)
        {
            Debug.LogWarning($"Item_Wagon: Item type {itemType} is not implemented.");
            return;
        }

        isPurchased = true;
        if (!PlayerInventory.Instance.TrySpendCoins(itemData.itemPrice))
        {
            isPurchased = false;
            Debug.LogWarning("Not enough coins to buy this item.");
            return;
        }

        if (itemType == ItemTypeWagon.HPFunctional)
        {
            Spawner_Wagon.Instance.wagonHPFunctionalStart += itemModifierValue;
        }
        else
        {
            Spawner_Wagon.Instance.wagonHPBrokenStart += itemModifierValue;
        }

        PlayerInventory.Instance.MarkItemAsBought(itemData.itemID);
        Debug.Log($"Purchased {itemData.itemID} {itemData.itemName} to increase wagon HP by {itemModifierValue}.");
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}