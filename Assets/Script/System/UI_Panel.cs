using UnityEngine;

public class UI_Panel : MonoBehaviour
{
    public void DisablePanel(){
        gameObject.SetActive(false);
    }

    public void EnablePanel(){
        gameObject.SetActive(true);
    }

    public void OnSubmit()
    {
        UI_UnivConfirmPanel.Instance.OnShow(ConfirmAction);
    }

    private void ConfirmAction()
    {
        Debug.Log($"You just bought {gameObject.name}!");
        // Perform the action here
    }
}
