using UnityEngine;
using UnityEngine.Events;

public class VNPanel : MonoBehaviour
{
    public VNDialogueSystem dialogueSubsystem;
    public UnityEvent onPanelDisplay;
    public bool isChangeSceneAfterDialogue;

    public void OnDisablePanel()
    {
        if (dialogueSubsystem != null) onPanelDisplay.Invoke();
        Manager_Game_GameScene.Instance.SetGameType(GameType.Gameplay);
        dialogueSubsystem.audioInvoker.StopBGM();
    }

    public void OnChangeScene(string sceneName)
    {
        if (isChangeSceneAfterDialogue)
        {
            SceneLoader.Instance.LoadScene(sceneName);
        }
    }
}