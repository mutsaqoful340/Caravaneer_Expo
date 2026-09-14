using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Universal_Interact : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("Determines if the object can be interacted with.")]
    public bool isInteractable = true;
    [Tooltip("If enabled, this object destroys itself after successful interaction.")]
    public bool destroyAfterInteract;
    [Tooltip("Determines whether one or two players are required to complete this interaction.")]
    public InteractionType interactionType = InteractionType.One;

    [Header("Hold Interaction Settings")]
    [Tooltip("Determines if the interaction requires holding the action button.")]
    public bool isHoldInteract;
    // [Tooltip("The animator component for the interactable object.")]
    // public Animator animator;
    [Tooltip("Delay before destroying this object after interaction.")]
    public float destroyDelay = 0f;
    [Tooltip("Duration in seconds for hold interaction.")]
    public float holdDuration = 2f; // Duration in seconds for hold interaction

    [Header("Interaction References")]
    public GameObject interactVisualPrefab; // Prefab for the interaction visual
    public GameObject twoInteractCover;
    public UI_InteractVisual interactVisualComponent; // Reference to the UI_InteractVisual component

    [Header("Interaction Events")]
    public UnityEvent onInteract;

    [Header("Debug")]
    [Tooltip("DO NOT MANUALLY TICK - Indicates whether the object is currently being interacted with.")]
    public bool isInteracting = false;

    public PlayerComponent CurrentInteractor { get; private set; }

    private Coroutine holdInteractionRoutine;
    private float interactionCurrentTime;

    // Two-player (dual) hold interaction state, tracked independently per role.
    private PlayerComponent mechanicInteractor;
    private PlayerComponent mercenaryInteractor;
    private Coroutine mechanicRoutine;
    private Coroutine mercenaryRoutine;
    private float mechanicCurrentTime;
    private float mercenaryCurrentTime;
    private bool mechanicReachedMax;
    private bool mercenaryReachedMax;

    private void Start()
    {
        GameObject visual = Instantiate(interactVisualPrefab, transform.position, Quaternion.identity);
        interactVisualComponent = visual.GetComponentInChildren<UI_InteractVisual>();
        interactVisualComponent.interactionType = interactionType; // must be set before the visual's own Start() reads it
        visual.transform.SetParent(transform);
    }

    public void OnCoverEnable()
    {
        twoInteractCover.SetActive(true);
    }

    public void OnCoverDisable()
    {
        twoInteractCover.SetActive(false);
    }
    
    public void Interact()
    {
        BeginInteraction(null);
    }

    public void BeginInteraction()
    {
        BeginInteraction(null);
    }

    public void BeginInteraction(PlayerComponent interactor)
    {
        if (!isInteractable) return;

        if (interactionType == InteractionType.Two)
        {
            BeginTwoPlayerInteraction(interactor);
            return;
        }

        if (isInteracting) return;

        CurrentInteractor = interactor;
        isInteracting = true;
        interactionCurrentTime = 0f;
        interactVisualComponent.OnActivateVisual();

        if (isHoldInteract)
        {
            interactVisualComponent?.SetHoldProgress(0f);
            holdInteractionRoutine = StartCoroutine(HoldInteractionRoutine());
            Debug.Log($"{gameObject.name} is being held for interaction.");
            return;
        }

        onInteract.Invoke();
        TryDestroyAfterInteract();
        isInteracting = false;
        CurrentInteractor = null;
        Debug.Log($"{gameObject.name} was interacted with.");
    }

    public void EndInteraction()
    {
        EndInteraction(null);
    }

    public void EndInteraction(PlayerComponent interactor)
    {
        if (interactionType == InteractionType.Two)
        {
            EndTwoPlayerInteraction(interactor);
            return;
        }

        if (!isInteracting) return;

        if (holdInteractionRoutine != null)
        {
            StopCoroutine(holdInteractionRoutine);
            holdInteractionRoutine = null;
        }

        interactVisualComponent.OnDeactivateVisual();
        isInteracting = false;
        interactionCurrentTime = 0f;
        CurrentInteractor = null;
        interactVisualComponent?.SetHoldProgress(0f);

        if (isHoldInteract)
        {
            Debug.Log($"{gameObject.name} hold interaction ended.");
        }
    }

    private IEnumerator HoldInteractionRoutine()
    {
        while (isInteracting && interactionCurrentTime < holdDuration)
        {
            interactionCurrentTime += Time.deltaTime;
            float progress = holdDuration > 0f ? interactionCurrentTime / holdDuration : 1f;
            interactVisualComponent?.SetHoldProgress(progress);
            Debug.Log($"{gameObject.name} hold timer: {interactionCurrentTime:F2}s / {holdDuration:F2}s");
            yield return null;
        }

        if (!isInteracting || !isInteractable)
        {
            holdInteractionRoutine = null;
            CurrentInteractor = null;
            yield break;
        }

        interactVisualComponent?.SetHoldProgress(1f);
        onInteract.Invoke();
        TryDestroyAfterInteract();
        Debug.Log($"{gameObject.name} hold interaction completed.");
        interactionCurrentTime = 0f;
        isInteracting = false;
        holdInteractionRoutine = null;
        CurrentInteractor = null;
    }

    // ---------- Two-player (dual) hold interaction ----------
    // Each role holds and progresses independently; onInteract fires only once both reach max fill.

    private void BeginTwoPlayerInteraction(PlayerComponent interactor)
    {
        if (interactor == null) return;

        InteractorRole role = interactor.isMercenary ? InteractorRole.Mercenary : InteractorRole.Mechanic;

        if (GetRoleInteractor(role) != null) return; // that role is already holding

        SetRoleInteractor(role, interactor);
        SetRoleCurrentTime(role, 0f);
        SetRoleReachedMax(role, false);

        if (!isInteracting)
        {
            isInteracting = true;
            interactVisualComponent.OnActivateVisual();
            // Both halves always start empty, even if only one role begins holding.
            interactVisualComponent?.SetHoldProgress(InteractorRole.Mechanic, 0f);
            interactVisualComponent?.SetHoldProgress(InteractorRole.Mercenary, 0f);
        }
        else
        {
            interactVisualComponent?.SetHoldProgress(role, 0f);
        }

        SetRoleRoutine(role, StartCoroutine(TwoPlayerHoldRoutine(role)));
        Debug.Log($"{gameObject.name} {role} is being held for interaction.");
    }

    private IEnumerator TwoPlayerHoldRoutine(InteractorRole role)
    {
        while (GetRoleInteractor(role) != null && GetRoleCurrentTime(role) < holdDuration)
        {
            SetRoleCurrentTime(role, GetRoleCurrentTime(role) + Time.deltaTime);
            float progress = holdDuration > 0f ? GetRoleCurrentTime(role) / holdDuration : 1f;
            interactVisualComponent?.SetHoldProgress(role, progress);
            yield return null;
        }

        if (GetRoleInteractor(role) == null)
        {
            yield break; // cancelled by an early release
        }

        SetRoleReachedMax(role, true);
        interactVisualComponent?.SetHoldProgress(role, 1f);
        SetRoleRoutine(role, null);
        Debug.Log($"{gameObject.name} {role} reached max hold progress.");

        if (mechanicReachedMax && mercenaryReachedMax)
        {
            onInteract.Invoke();
            TryDestroyAfterInteract();
            Debug.Log($"{gameObject.name} two-player hold interaction completed.");
            ResetTwoPlayerState();
        }
    }

    private void EndTwoPlayerInteraction(PlayerComponent interactor)
    {
        if (interactor == null) return;

        InteractorRole role = interactor.isMercenary ? InteractorRole.Mercenary : InteractorRole.Mechanic;

        if (GetRoleInteractor(role) != interactor) return;

        Coroutine routine = role == InteractorRole.Mechanic ? mechanicRoutine : mercenaryRoutine;
        if (routine != null)
        {
            StopCoroutine(routine);
        }

        SetRoleRoutine(role, null);
        SetRoleInteractor(role, null);
        SetRoleCurrentTime(role, 0f);
        SetRoleReachedMax(role, false);
        interactVisualComponent?.SetHoldProgress(role, 0f);
        Debug.Log($"{gameObject.name} {role} hold interaction ended.");

        if (mechanicInteractor == null && mercenaryInteractor == null && isInteracting)
        {
            isInteracting = false;
            interactVisualComponent.OnDeactivateVisual();
        }
    }

    private void ResetTwoPlayerState()
    {
        mechanicInteractor = null;
        mercenaryInteractor = null;
        mechanicRoutine = null;
        mercenaryRoutine = null;
        mechanicCurrentTime = 0f;
        mercenaryCurrentTime = 0f;
        mechanicReachedMax = false;
        mercenaryReachedMax = false;
        isInteracting = false;
        interactVisualComponent.OnDeactivateVisual();
    }

    private PlayerComponent GetRoleInteractor(InteractorRole role) =>
        role == InteractorRole.Mechanic ? mechanicInteractor : mercenaryInteractor;

    private float GetRoleCurrentTime(InteractorRole role) =>
        role == InteractorRole.Mechanic ? mechanicCurrentTime : mercenaryCurrentTime;

    private void SetRoleInteractor(InteractorRole role, PlayerComponent interactor)
    {
        if (role == InteractorRole.Mechanic) mechanicInteractor = interactor;
        else mercenaryInteractor = interactor;
    }

    private void SetRoleCurrentTime(InteractorRole role, float value)
    {
        if (role == InteractorRole.Mechanic) mechanicCurrentTime = value;
        else mercenaryCurrentTime = value;
    }

    private void SetRoleReachedMax(InteractorRole role, bool value)
    {
        if (role == InteractorRole.Mechanic) mechanicReachedMax = value;
        else mercenaryReachedMax = value;
    }

    private void SetRoleRoutine(InteractorRole role, Coroutine routine)
    {
        if (role == InteractorRole.Mechanic) mechanicRoutine = routine;
        else mercenaryRoutine = routine;
    }

    private void TryDestroyAfterInteract()
    {
        if (!destroyAfterInteract) return;

        isInteractable = false;
        float delay = Mathf.Max(0f, destroyDelay);
        Destroy(gameObject, delay);
    }
}