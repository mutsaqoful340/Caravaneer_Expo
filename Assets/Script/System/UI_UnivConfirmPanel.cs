using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public enum PanelState
{
    Confirm,
    Cancel
}
public class UI_UnivConfirmPanel : MonoBehaviour
{
    public static UI_UnivConfirmPanel Instance { get; set; }
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private GameObject panel;
    public bool isActive;

    private Action onConfirm;
    private Action onCancel;

    private CanvasGroup callerCanvas;
    private GameObject previousSelection;
    private Selectable fallbackSelection;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        gameObject.SetActive(false);
    }

    public void OnShow(
        Action confirmAction,
        Action cancelAction = null,
        CanvasGroup callerCanvasToFreeze = null,
        Selectable fallbackSelectable = null)
    {
        callerCanvas = callerCanvasToFreeze;
        previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        fallbackSelection = fallbackSelectable;

        if (callerCanvas != null)
        {
            callerCanvas.interactable = false;
        }
        
        SelectFirstButton();

        onConfirm = confirmAction;
        onCancel = cancelAction;

        gameObject.SetActive(true);

        confirmButton.onClick.RemoveAllListeners();
        cancelButton.onClick.RemoveAllListeners();

        confirmButton.onClick.AddListener(Confirm);
        cancelButton.onClick.AddListener(Cancel);
    }

    private void Confirm()
    {
        Action action = onConfirm;

        gameObject.SetActive(false);   // hide panel first
        onConfirm = null;
        onCancel = null;

        action?.Invoke();              // let UseItem()/Destroy() happen now

        RestoreCallerInteraction();    // check activeInHierarchy after destruction is real
    }

    public void Cancel()
    {
        Action action = onCancel;

        Close();

        action?.Invoke();
    }

    private void Close()
    {
        onConfirm = null;
        onCancel = null;
        
        RestoreCallerInteraction();
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        onConfirm = null;
        onCancel = null;

        confirmButton.onClick.RemoveAllListeners();
        cancelButton.onClick.RemoveAllListeners();
    }

    private void SelectFirstButton()
    {
        if (confirmButton != null)
        {
            confirmButton.Select();
        }
    }

    private void RestoreCallerInteraction()
    {
        if (callerCanvas == null && fallbackSelection == null) return;

        MonoBehaviour coroutineHost = Manager_UI.Instance != null ? Manager_UI.Instance : this;
        coroutineHost.StartCoroutine(RestoreCallerInteractionNextFrame(callerCanvas, previousSelection, fallbackSelection));

        callerCanvas = null;
        previousSelection = null;
        fallbackSelection = null;
    }

    private IEnumerator RestoreCallerInteractionNextFrame(
        CanvasGroup canvas,
        GameObject previous,
        Selectable fallback)
    {
        yield return null;

        if (canvas != null)
        {
            canvas.interactable = true;
        }

        if (EventSystem.current == null) yield break;

        Selectable previousSelectable = previous != null ? previous.GetComponent<Selectable>() : null;
        if (IsValidSelection(previousSelectable))
        {
            EventSystem.current.SetSelectedGameObject(previous);
            Debug.Log($"Restored previous selection: {previous.name}");
            yield break;
        }

        if (canvas != null)
        {
            foreach (Selectable selectable in canvas.GetComponentsInChildren<Selectable>(true))
            {
                if (!IsValidSelection(selectable)) continue;

                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
                Debug.Log($"Restored first selectable in caller canvas: {selectable.name}");
                yield break;
            }
        }

        if (IsValidSelection(fallback))
        {
            EventSystem.current.SetSelectedGameObject(fallback.gameObject);
            Debug.Log($"Restored fallback selection: {fallback.name}");
        }
    }

    private static bool IsValidSelection(Selectable selectable)
    {
        return selectable != null && selectable.gameObject.activeInHierarchy &&
            selectable.enabled && selectable.IsInteractable();
    }
}