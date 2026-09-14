using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Item/Item Data", order = 1)]
public class ItemData : ScriptableObject
{
    public string itemName;
    [Tooltip("01 Functional Wagon HP, 02 Broken Wagon HP, 03 Pia HP, 04 Pippa HP")]
    public string itemID;
    public int itemPrice;
}