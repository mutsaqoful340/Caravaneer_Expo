using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;

public enum VNDisplayMode
{
    LeftSide,
    RightSide
}

public class VNDialogueSystem : MonoBehaviour
{
    public static VNDialogueSystem Instance { get; set; }
    public float skipAllHoldDuration = 2f; // Duration to hold the skip button to skip all dialogues
    public VNData VNData;
    public Image imageBackground;
    public Image holdSkipDialogueIndicator;
    public bool isDialogueActive = false;
    // public bool isChangeSceneAfterDialogue = false;
    public bool showDialogue = false;
    public UnityEvent onDialogueFinished;
    public Audio_Invoker audioInvoker;

    private Coroutine skipHoldRoutine;
    private bool isSkipButtonHeld;
    private bool skipAllTriggered;
    private bool skipCurrentDialogueRequested;

    // Serves as reference to the current character data being displayed
    [Header("Left Side Panel Settings")]
    public Canvas leftPanel;
    public Animator leftPanelAnimator;
    public Image leftCharacterSprite;
    public TextMeshProUGUI leftCharacterName;
    public TextMeshProUGUI leftCharacterDialogue;
    public bool isLeftSide = false; // Determines if the dialogue should be displayed on the left side

    // Serves as reference to the current character data being displayed
    [Header("Right Side Panel Settings")]
    public Canvas rightPanel;
    public Animator rightPanelAnimator;
    public Image rightCharacterSprite;
    public TextMeshProUGUI rightCharacterName;
    public TextMeshProUGUI rightCharacterDialogue;
    public bool isRightSide = false; // Determines if the dialogue should be displayed on the right side

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        // Ensure panels are inactive at the start
        if (leftPanel != null) leftPanel.gameObject.SetActive(false);
        if (rightPanel != null) rightPanel.gameObject.SetActive(false);
        if (leftPanelAnimator == null) leftPanelAnimator = leftPanel.GetComponent<Animator>();
        if (rightPanelAnimator == null) rightPanelAnimator = rightPanel.GetComponent<Animator>();
        if (imageBackground != null) imageBackground.gameObject.SetActive(false);
        if (holdSkipDialogueIndicator != null) holdSkipDialogueIndicator.fillAmount = 0f;
        SetSkipHoldProgress(0f);
        // if (skipButton != null) Manager_UI.Instance.SelectFirstButtonInPanel(skipButton);
    }

    public void OnPlayDialogue()
    {
        if (VNData == null || VNData.VNDialogue == null || VNData.VNDialogue.Length == 0)
        {
            Debug.LogWarning("No VNData dialogue entries available!");
            return;
        }
        if (isDialogueActive)
        {
            Debug.LogWarning("Dialogue is already active!");
            return;
        }
        Manager_Game_GameScene.Instance.SetGameType(GameType.VN);
        isDialogueActive = true;
        skipCurrentDialogueRequested = false;
        skipAllTriggered = false;
        StartCoroutine(DisplayDialogue());
        audioInvoker.OnPlaySFXLocal("BGMVN", SFXType.BGM, 0.13f);
    }

    private IEnumerator DisplayDialogue()
    {
        int dialogueIndex = 0;
        VNDisplayMode? lastMode = null; // Tracks the previously shown panel to detect consecutive same-side dialogue
        bool hasLeftAppeared = false;
        bool hasRightAppeared = false;

        foreach (VNDialoguesEntry dialogueEntry in VNData.VNDialogue)
        {
            Debug.Log($"Displaying dialogue {dialogueIndex} of {VNData.VNDialogue.Length}");
            dialogueIndex++;
            
            // Null check for character data
            if (dialogueEntry == null || dialogueEntry.dialogueCharacter == null)
            {
                Debug.LogWarning("Character data or VNData is null, skipping dialogue entry!");
                continue;
            }
               
               yield return new WaitForSeconds(dialogueEntry.dialogueDisplayDelay);

            bool isSamePanelAsLast = lastMode.HasValue && lastMode.Value == dialogueEntry.displayMode;

            if (imageBackground != null && dialogueEntry.backgroundImage != null)
            {
                imageBackground.sprite = dialogueEntry.backgroundImage;
                imageBackground.gameObject.SetActive(true);
            }

            if (dialogueEntry.displayMode == VNDisplayMode.LeftSide)
            {
                isLeftSide = true;
                if (!isSamePanelAsLast)
                {
                    leftPanel.sortingOrder = 4;
                    rightPanel.sortingOrder = 3;

                    if (lastMode == VNDisplayMode.RightSide)
                    {
                        isRightSide = false;
                        rightPanelAnimator.SetTrigger("Hide");
                    }

                    OnPanelShow_Left(!hasLeftAppeared);
                    hasLeftAppeared = true;
                }

                if (leftCharacterSprite != null && leftCharacterName != null && leftCharacterDialogue != null)
                {
                    leftCharacterSprite.sprite = dialogueEntry.dialogueCharacter.characterSprite;
                    leftCharacterSprite.SetNativeSize();
                    leftCharacterName.text = dialogueEntry.dialogueCharacter.characterName;
                    leftCharacterDialogue.text = dialogueEntry.dialogueText;
                }
                else
                {
                    Debug.LogWarning("Left panel UI elements are not assigned!");
                }
            }
            else if (dialogueEntry.displayMode == VNDisplayMode.RightSide)
            {
                isRightSide = true;
                if (!isSamePanelAsLast)
                {
                    rightPanel.sortingOrder = 4;
                    leftPanel.sortingOrder = 3;

                    if (lastMode == VNDisplayMode.LeftSide)
                    {
                        isLeftSide = false;
                        leftPanelAnimator.SetTrigger("Hide");
                    }

                    OnPanelShow_Right(!hasRightAppeared);
                    hasRightAppeared = true;
                }

                if (rightCharacterSprite != null && rightCharacterName != null && rightCharacterDialogue != null)
                {
                    rightCharacterSprite.sprite = dialogueEntry.dialogueCharacter.characterSprite;
                    rightCharacterSprite.SetNativeSize();
                    rightCharacterName.text = dialogueEntry.dialogueCharacter.characterName;
                    rightCharacterDialogue.text = dialogueEntry.dialogueText;
                }
                else
                {
                    Debug.LogWarning("Right panel UI elements are not assigned!");
                }
            }

            lastMode = dialogueEntry.displayMode;

            // Wait for a short duration before displaying the next dialogue
            skipCurrentDialogueRequested = false;
            float currentDuration = 0f;
            while (currentDuration < dialogueEntry.dialogueDisplayDuration && !skipCurrentDialogueRequested)
            {
                currentDuration += Time.deltaTime;
                yield return null;
            }
        }

        OnPanelHide_All();
        Debug.Log("Dialogue sequence finished!");
        isDialogueActive = false;
        showDialogue = false;
    }

    private void Update()
    {
        if (showDialogue && !isDialogueActive)
        {
            showDialogue = false;
            OnPlayDialogue();
        }
    }

    private void OnPanelShow_Right(bool isFirstAppearance = false)
    {
        rightPanel.gameObject.SetActive(true);
        rightPanelAnimator.SetTrigger(isFirstAppearance ? "Enable" : "Show");
    }

    private void OnPanelShow_Left(bool isFirstAppearance = false)
    {
        leftPanel.gameObject.SetActive(true);
        leftPanelAnimator.SetTrigger(isFirstAppearance ? "Enable" : "Show");
    }

    public void OnPanelDisable_Left()
    {
        if (leftPanel)
        {
            leftPanel.gameObject.SetActive(false);
            isLeftSide = false;
        }
    }

    public void OnPanelDisable_Right()
    {
        if (rightPanel)
        {
            rightPanel.gameObject.SetActive(false);
            isRightSide = false;
        }
    }

    public void OnPanelHide_All()
    {
        if (leftPanel)
        {
            leftPanelAnimator.SetTrigger("Disable");
            isLeftSide = false;
        }
        if (rightPanel)
        {
            rightPanelAnimator.SetTrigger("Disable");
            isRightSide = false;
        }

        imageBackground.gameObject.SetActive(false);
        onDialogueFinished?.Invoke();

        // SceneLoader.Instance.LoadScene("Gameplay");
    }

    public void OnSkipDialogue()
    {
        UI_UnivConfirmPanel.Instance.OnShow(
            () => OnConfirmSkipDialogue(),
            () => Debug.Log("Dialogue skip canceled."),
            null
        );
    }

    public void OnConfirmSkipDialogue()
    {
        StopAllCoroutines();
        ClearSkipInputState();
        OnPanelHide_All();
        isDialogueActive = false;
        showDialogue = false;
        Debug.Log("Dialogue skipped!");
    }

    public void OnBeginSkipDialogue()
    {
        if (!isDialogueActive || skipHoldRoutine != null)
        {
            return;
        }

        isSkipButtonHeld = true;
        skipAllTriggered = false;
        SetSkipHoldProgress(0f);
        skipHoldRoutine = StartCoroutine(WaitForSkipConfirmation());
    }

    private IEnumerator WaitForSkipConfirmation()
    {
        float currentHoldTime = 0f;

        while (isSkipButtonHeld && currentHoldTime < skipAllHoldDuration)
        {
            currentHoldTime += Time.deltaTime;
            SetSkipHoldProgress(skipAllHoldDuration > 0f ? currentHoldTime / skipAllHoldDuration : 1f);
            yield return null;
        }

        skipHoldRoutine = null;

        if (!isSkipButtonHeld)
        {
            yield break;
        }

        skipAllTriggered = true;
        OnSkipDialogue();
    }

    public void OnEndSkipDialogue()
    {
        if (!isSkipButtonHeld)
        {
            return;
        }

        isSkipButtonHeld = false;
        SetSkipHoldProgress(0f);

        if (skipHoldRoutine != null)
        {
            StopCoroutine(skipHoldRoutine);
            skipHoldRoutine = null;
        }

        if (!skipAllTriggered)
        {
            skipCurrentDialogueRequested = true;
        }
    }

    public void OnPlayPopupOP()
    {
        PopupPlayer.Instance.OnPlayPopup_OP();
    }

    public void OnPlayPopupED()
    {
        PopupPlayer.Instance.OnPlayPopup_ED();
    }

    public void OnPlayComic()
    {
        Comic_Player.Instance.OnPlayComic();
    }

    public void ShowButtonTutorial()
    {
        VNPlayer.Instance.ShowButtonTutorial();
    }

    public void HideButtonTutorial()
    {
        VNPlayer.Instance.HideButtonTutorial();
    }

    public void DestroyVN()
    {
        Destroy(gameObject);
    }

    public void OnChangeScene(string sceneName)
    {
        SceneLoader.Instance.LoadScene(sceneName);
    }

    private void ClearSkipInputState()
    {
        isSkipButtonHeld = false;
        skipAllTriggered = false;
        skipCurrentDialogueRequested = false;
        skipHoldRoutine = null;
        SetSkipHoldProgress(0f);
    }

    private void SetSkipHoldProgress(float progress)
    {
        if (holdSkipDialogueIndicator != null)
        {
            holdSkipDialogueIndicator.fillAmount = Mathf.Clamp01(progress);
        }
    }
}