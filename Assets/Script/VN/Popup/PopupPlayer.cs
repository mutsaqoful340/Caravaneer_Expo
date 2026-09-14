using UnityEngine;

public class PopupPlayer : MonoBehaviour
{
    public static PopupPlayer Instance {set; get;}
    public GameObject popup_OP_Prefab;
    public GameObject popup_ED_Prefab;

    private void Awake()
    {
        Instance = this;
    }
    
    public void OnPlayPopup_OP()
    {
        if (popup_OP_Prefab)
        {
            Instantiate(popup_OP_Prefab, transform);
        }
    }

    public void OnPlayPopup_ED()
    {
        if (popup_ED_Prefab)
        {
            Instantiate(popup_ED_Prefab, transform);
        }
    }

    public void OnPlayPopup(GameObject popupPrefab)
    {
        if (popupPrefab)
        {
            Instantiate(popupPrefab, transform);
        }
    }
}
