using UnityEngine;
using System;

[Serializable]
public class VNDialoguesEntry
{
    public PopupData dialogueCharacter;
    public string dialogueText;
    public float dialogueDisplayDelay;
    public float dialogueDisplayDuration;
    public VNDisplayMode displayMode;
    public Sprite backgroundImage;
}

[CreateAssetMenu(fileName = "VNData", menuName = "VN Data", order = 1)]
public class VNData : ScriptableObject
{
    public VNDialoguesEntry[] VNDialogue;
}