using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class PopupPanel : MonoBehaviour
{
    public PopupDialogueSystem dialogueSystem;
    public Animator animator;

    public void OnPanelDisable_Left()
    {
        dialogueSystem.PrvPanelHide_Left();
    }

        public void OnPanelDisable_Right()
    {
        dialogueSystem.PrvPanelHide_Right();
    }
}