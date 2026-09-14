using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[System.Serializable]
public class PanelReference
{
    public string name;
    public GameObject gameObject;
    public bool hasAnimation;
}

public class Manager_UI : MonoBehaviour
{
    public static Manager_UI Instance { get; private set; }
    [Header("UI Panels")]
    [Tooltip("Panels to manage by name and GameObject reference.")]
    public PanelReference[] panels;
    public GameObject[] panelHistory;

    private GameObject currentActivePanel;
    
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // disables every panels at start
        foreach (PanelReference panelReference in panels)
        {
            if (panelReference?.gameObject == null)
            {
                continue;
            }

            panelReference.gameObject.SetActive(false);
            SetPanelButtonsInteractable(panelReference.gameObject, false);
        }

        panelHistory = Array.Empty<GameObject>();
    }

    // Show a specific panel by name.
    public void OnShowPanel(string panelName)
    {
        Debug.Log($"Attempting to show panel: {panelName}");
        PanelReference selectedPanel = null;

        foreach (PanelReference panelReference in panels)
        {
            if (panelReference != null && panelReference.name == panelName)
            {
                selectedPanel = panelReference;
                break;
            }
        }

        if (selectedPanel == null || selectedPanel.gameObject == null)
        {
            Debug.LogError("No panel found with name: " + panelName);
            return;
        }

        GameObject panel = selectedPanel.gameObject;

        if (currentActivePanel != null)
        {
            AddPanelToHistory(currentActivePanel);
            SetPanelButtonsInteractable(currentActivePanel, false);
        }

        currentActivePanel = panel;
        SetPanelButtonsInteractable(currentActivePanel, true);
        Animator animator = currentActivePanel.GetComponent<Animator>();
        if (animator != null && selectedPanel.hasAnimation)
        {
            currentActivePanel.SetActive(true);
            animator?.SetTrigger("Open");
        }
        else
        {
            currentActivePanel.SetActive(true);
        }

        SelectFirstButtonInPanel(currentActivePanel);
        Manager_Game.Instance.SetState(GameState.UI);

        // Animator animator = currentActivePanel.GetComponent<Animator>();
        // if (animator != null)
        // {
        //     animator.Play("Open");
        // }
    }

    public void OnCloseCurrentPanel()
    {
        if (currentActivePanel != null)
        {
            SetPanelButtonsInteractable(currentActivePanel, false);

            PanelReference currentPanelReference = FindPanelReference(currentActivePanel);
            Animator animator = currentActivePanel.GetComponent<Animator>();

            if (animator != null && currentPanelReference != null && currentPanelReference.hasAnimation)
            {
                animator.ResetTrigger("Open");
                animator.SetTrigger("Close");
            }
            else
            {
                currentActivePanel.SetActive(false);
            }
        }

        GameObject previousPanel = RemoveLastPanelFromHistory();
        if (previousPanel == null)
        {
            currentActivePanel = null;
            ClearSelectedButton();
            Manager_Game.Instance?.SetState(GameState.Gameplay);
            return;
        }

        currentActivePanel = previousPanel;
        SetPanelButtonsInteractable(currentActivePanel, true);
        currentActivePanel.SetActive(true);
        SelectFirstButtonInPanel(currentActivePanel);
    }

    private PanelReference FindPanelReference(GameObject panel)
    {
        foreach (PanelReference panelReference in panels)
        {
            if (panelReference != null && panelReference.gameObject == panel)
            {
                return panelReference;
            }
        }

        return null;
    }

    public void OnCloseAllPanels()
    {
        foreach (PanelReference panelReference in panels)
        {
            if (panelReference == null || panelReference.gameObject == null)
            {
                continue;
            }

            GameObject panel = panelReference.gameObject;
            SetPanelButtonsInteractable(panel, false);
            Animator animator = panel.GetComponent<Animator>();
            // UI_Panel uI_Panel = panel.GetComponent<UI_Panel>();

            if (animator != null && panelReference.hasAnimation)
            {
                animator.ResetTrigger("Open");
                animator.SetTrigger("Close"); Debug.Log($"Closing panel with animation: {panelReference.name}");
                ClearSelectedButton();
            }
            else
            {
                panel.SetActive(false);
                ClearSelectedButton();
            }
        }

        currentActivePanel = null;
        panelHistory = Array.Empty<GameObject>();
        ClearSelectedButton();
        RestorePlayerControl();
    }

    private void AddPanelToHistory(GameObject panel)
    {
        if (panel == null)
        {
            return;
        }

        int historyLength = panelHistory?.Length ?? 0;
        Array.Resize(ref panelHistory, historyLength + 1);
        panelHistory[historyLength] = panel;
    }

    private GameObject RemoveLastPanelFromHistory()
    {
        if (panelHistory == null || panelHistory.Length == 0)
        {
            return null;
        }

        int lastIndex = panelHistory.Length - 1;
        GameObject previousPanel = panelHistory[lastIndex];
        Array.Resize(ref panelHistory, lastIndex);
        return previousPanel;
    }

    private void SetPanelButtonsInteractable(GameObject panel, bool interactable)
    {
        if (panel == null)
        {
            return;
        }

        Button[] buttons = panel.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }

    // === Specific Panel Methods ===

    public void SelectFirstButtonInPanel(GameObject panel)
    {
        if (panel == null)
        {
            return;
        }

        Button[] buttons = panel.GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            if (button != null && button.gameObject.activeInHierarchy && button.enabled && button.IsInteractable())
            {
                if (EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(button.gameObject);
                }

                return;
            }
        }
    }

    private void ClearSelectedButton()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void RestorePlayerControl(){
        Manager_Game.Instance.SetState(GameState.Gameplay);
    }
}