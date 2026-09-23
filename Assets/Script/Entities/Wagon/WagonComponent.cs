using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum WagonState // Two-stage wagon state: Functional and Broken
{
    Functional,
    Broken,
    Destroyed
}

public class WagonComponent : MonoBehaviour
{
    public static WagonComponent Instance { get; set; }
    [Header("Wagon Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float wheelRotationSpeed = 360f;
    [SerializeField] private Vector3 wheelRotationAxis = Vector3.forward;
    [Tooltip("Caps how often the wheel rotation visually updates, for a stepped/low-FPS animation look.")]
    [SerializeField] private float wheelAnimationFPS = 12f;
    public int startHPFunctional = 5;
    public int startHPBroken = 8;
    public WagonState currentWagonState = WagonState.Functional;
    public float IFrameDuration = 1f;
    public UnityEvent onWagonBroken;

    [Header("Reapair Settings")]
    [Tooltip("Time held before checking whether the wagon can be repaired.")]
    public float repairEligibilityCheckDuration = 0.5f;
    [Tooltip("Holding the action button for at least this long triggers a repair instead of mounting.")]
    public float repairHoldThreshold = 0.5f;
    [Tooltip("Repair materials consumed and HP restored per long-press repair.")]
    public int repairCost = 1;
    public GameObject repairIcon;

    [Header("Wagon References")]
    public GameObject wagonFunctionalVisual;
    public GameObject wagonBrokenVisual;
    public Animator animator;
    public GameObject rodaKanan;
    public GameObject rodaKiri;
    public Canvas wagonHPCanvas;
    public GameObject heartFunctionalPrefab;
    public GameObject heartBrokenPrefab;
    public Audio_Invoker audioInvoker;
    public Wagon_Breaker wagonBreaker;

    [Header("Player References")]
    public PlayerComponent mechanic;
    public Transform slotMechanic;
    public bool isMechanicMounted = false;
    public PlayerComponent mercenary;
    public Transform slotMercenary;
    public bool isMercenaryMounted = false;

    [Header("Debug")]
    [SerializeField] private int currHPBroken = 8;
    [SerializeField] private int currHPFunctional = 5;
    [SerializeField] private bool isBroken = false;
    [SerializeField] private bool isRepairingFunctionalHP = false;
    [SerializeField] public bool isDestroyed = false;
    [SerializeField] private bool isIFrame = false;
    [SerializeField] private CharacterController mechanicCC;
    [SerializeField] private Rigidbody mechanicRB;
    [SerializeField] private CharacterController mercenaryCC;
    [SerializeField] private Rigidbody mercenaryRB;

    private PlayerComponent pressingPlayer;
    private float pressStartTime;
    private Coroutine holdRepairRoutine;
    private bool repairTriggeredThisPress;
    private float wheelAnimationTimer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (animator == null) animator = GetComponent<Animator>();
        currHPFunctional = startHPFunctional;
        currHPBroken = startHPBroken;
        // UpdateWagonStateAnimation();
        UpdateHPUI();
    }

    private void Start()
    {
        if (repairIcon != null) repairIcon.SetActive(false);
        UpdateWagonVisuals();
        if (wagonBreaker != null) wagonBreaker.GetWagonStats();
    }

    // Reapplies starting HP after Awake, so spawners can carry over upgrades bought before this wagon was instantiated.
    public void InitializeStartingHP(int functionalStart, int brokenStart)
    {
        startHPFunctional = functionalStart;
        startHPBroken = brokenStart;
        currHPFunctional = functionalStart;
        currHPBroken = brokenStart;
        UpdateHPUI();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    #region Core Methods
    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.name.Contains("Player_Mechanic"))
            {
                mechanic = other.GetComponent<PlayerComponent>();
                mechanicCC = mechanic.GetComponent<CharacterController>();
                mechanicRB = mechanic.GetComponent<Rigidbody>();
            }
            else if (other.name.Contains("Player_Mercenary"))
            {
                mercenary = other.GetComponent<PlayerComponent>();
                mercenaryCC = mercenary.GetComponent<CharacterController>();
                mercenaryRB = mercenary.GetComponent<Rigidbody>();
            }
        }

        if (other.CompareTag("RepairMaterial"))
        {
            RepairMaterial repairMaterial = other.GetComponent<RepairMaterial>();
            if (repairMaterial != null)
            {
                OnRepair(repairMaterial.repairValue);
                Destroy(other.gameObject);
            }
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.name.Contains("Player_Mechanic") && !isMechanicMounted)
            {
                mechanic = null;
                mechanicCC = null;
                mechanicRB = null;
            }
            else if (other.name.Contains("Player_Mercenary") && !isMercenaryMounted)
            {
                mercenary = null;
                mercenaryCC = null;
                mercenaryRB = null;
            }
        }
    }

    public void BeginInteraction(PlayerComponent interactor)
    {
        if (interactor != mechanic && interactor != mercenary)
        {
            Debug.LogWarning($"{interactor.gameObject.name} is not assigned to this wagon.");
            return;
        }

        pressingPlayer = interactor;
        pressStartTime = Time.time;
        repairTriggeredThisPress = false;
        Debug.Log($"[WagonInteract] BeginInteraction by {interactor.gameObject.name} at t={Time.time:F2}");

        if (holdRepairRoutine != null)
        {
            StopCoroutine(holdRepairRoutine);
            SetRepairAnimation(pressingPlayer, false);
        }
        holdRepairRoutine = StartCoroutine(HoldRepairRoutine(interactor));
    }

    public void EndInteraction(PlayerComponent interactor)
    {
        Debug.Log($"[WagonInteract] EndInteraction called by {interactor.gameObject.name} at t={Time.time:F2}, pressingPlayer={(pressingPlayer ? pressingPlayer.gameObject.name : "null")}");

        if (pressingPlayer != interactor)
        {
            Debug.Log("[WagonInteract] EndInteraction ignored - pressingPlayer mismatch.");
            return;
        }

        pressingPlayer = null;
        SetRepairAnimation(interactor, false);

        if (holdRepairRoutine != null)
        {
            StopCoroutine(holdRepairRoutine);
            holdRepairRoutine = null;
            Debug.Log("[WagonInteract] Hold routine stopped by EndInteraction.");
        }

        if (!repairTriggeredThisPress)
        {
            TogglePlayerMount(interactor);
        }
    }

    private void TogglePlayerMount(PlayerComponent interactor)
    {
        if (interactor == mechanic)
        {
            if (isMechanicMounted)
            {
                OnMechanicDismount();
            }
            else if (!CanOperate())
            {
                Debug.LogWarning($"{gameObject.name} cannot mount until it is fully repaired.");
            }
            else
            {
                OnMechanicMount();
            }
        }
        else if (interactor == mercenary)
        {
            if (isMercenaryMounted)
            {
                OnMercenaryDismount();
            }
            else if (!CanOperate())
            {
                Debug.LogWarning($"{gameObject.name} cannot mount until it is fully repaired.");
            }
            else
            {
                OnMercenaryMount();
            }
        }
    }

    private void TryRepairFromInventory(PlayerComponent interactor)
    {
        bool interactorMounted =
            (interactor == mechanic && isMechanicMounted) ||
            (interactor == mercenary && isMercenaryMounted);

        if (interactorMounted)
        {
            Debug.LogWarning($"{interactor.gameObject.name} cannot repair the wagon while mounted.");
            return;
        }

        if (!NeedsRepair())
        {
            Debug.LogWarning($"{gameObject.name} does not need repair right now.");
            return;
        }

        if (PlayerInventory.Instance == null || !PlayerInventory.Instance.TrySpendRepairMaterials(repairCost))
        {
            Debug.LogWarning($"{interactor.gameObject.name} does not have enough repair materials.");
            return;
        }

        OnRepair(repairCost);
    }

    public void OnTakeDamage(int damage)
    {
        if (isDestroyed || damage <= 0 || isIFrame)
        {
            return;
        }

        CameraConstraint.Instance?.CameraShake();
        audioInvoker.OnPlaySFXLocal("ImpactWood", SFXType.OneShot);
        
        StartCoroutine(IFrameCoroutine());
        if (currentWagonState == WagonState.Functional)
        {
            int previousHP = currHPFunctional;
            currHPFunctional = Mathf.Max(0, currHPFunctional - damage);
            if (currHPFunctional <= 0)
            {
                currHPFunctional = 1;
                currHPBroken = 6;
                isBroken = true;
                isRepairingFunctionalHP = false;
                SetWagonState(WagonState.Broken);
                onWagonBroken?.Invoke();
                DismountAllPlayers();
                UpdateHPUI();
                return;
            }

            AnimateLostHearts(previousHP - currHPFunctional);
            return;
        }

        if (IsRepairingBrokenHP())
        {
            int previousHP = currHPBroken;
            currHPBroken = Mathf.Max(0, currHPBroken - damage);
            if (currHPBroken <= 0)
            {
                OnWagonDestroyed();
            }
            else
            {
                AnimateLostHearts(previousHP - currHPBroken);
            }
        }
        else
        {
            int previousHP = currHPFunctional;
            currHPFunctional = Mathf.Max(0, currHPFunctional - damage);
            if (currHPFunctional <= 0)
            {
                OnWagonDestroyed();
            }
            else
            {
                AnimateLostHearts(previousHP - currHPFunctional);
            }
        }
        Manager_GameLocal.Instance.OnCheckEntity();
    }

    public void OnRepair(int repairAmount)
    {
        if (isDestroyed || repairAmount <= 0 || isMercenaryMounted)
        {
            return;
        }

        if (currentWagonState == WagonState.Broken)
        {
            currHPBroken = Mathf.Min(currHPBroken + repairAmount, startHPBroken);

            if (currHPBroken >= startHPBroken)
            {
                SetWagonState(WagonState.Functional);
                isBroken = false;
                isRepairingFunctionalHP = false;
            }

            UpdateHPUI();
            return;
        }

        if (currentWagonState == WagonState.Functional && !isBroken)
        {
            currHPFunctional = Mathf.Clamp(
                currHPFunctional + repairAmount,
                1,
                startHPFunctional);
            UpdateHPUI();
        }
    }
    #endregion

    #region Helper Methods
    private void OnMechanicMount()
    {
        isMechanicMounted = true;
        mechanic.playerVisual.SetActive(false);
        mechanic.playerStatUI.SetActive(false);
        if (mechanicCC != null && mechanicRB != null)
        {
            mechanicCC.enabled = false;
            mechanicRB.isKinematic = true; // Make the Rigidbody kinematic to prevent physics interactions
            mechanic.isMounted = true;
        }
        else
        {
            Debug.LogWarning($"{mechanic.gameObject.name} does not have a CharacterController & RigidBody component.");
        }
        // mechanicController.enabled = true; // Re-enable the CharacterController to reset its state
        mechanic.gameObject.transform.SetParent(slotMechanic);
        mechanic.gameObject.transform.localPosition = Vector3.zero;
    }

    private void OnMercenaryMount()
    {
        isMercenaryMounted = true;
        mercenary.playerVisual.SetActive(false);
        mercenary.playerStatUI.SetActive(false);
        if (mercenaryCC != null && mercenaryRB != null)
        {
            mercenaryCC.enabled = false;
            mercenaryRB.isKinematic = true; // Make the Rigidbody kinematic to prevent physics interactions
            mercenary.isMounted = true;
        }
        else
        {
            Debug.LogWarning($"{mercenary.gameObject.name} does not have a CharacterController component.");
        }
        mercenary.gameObject.transform.SetParent(slotMercenary);
        mercenary.gameObject.transform.localPosition = Vector3.zero;
    }

    private void OnMechanicDismount()
    {
        isMechanicMounted = false;
        mechanic.playerVisual.SetActive(true);
        mechanic.playerStatUI.SetActive(true);
        if (mechanicCC != null && mechanicRB != null)
        {
            mechanicCC.enabled = true;
            mechanicRB.isKinematic = false; // Make the Rigidbody non-kinematic to allow physics interactions
            mechanic.isMounted = false;
        }
        else
        {
            Debug.LogWarning($"{mechanic.gameObject.name} does not have a CharacterController component.");
        }

        if (mechanic.prevParent != null)
        {
            mechanic.gameObject.transform.SetParent(mechanic.prevParent.transform, true);
        }
        else
        {
            mechanic.gameObject.transform.SetParent(null);
        }
    }

    private void OnMercenaryDismount()
    {
        isMercenaryMounted = false;
        mercenary.playerVisual.SetActive(true);
        mercenary.playerStatUI.SetActive(true);
        if (mercenaryCC != null && mercenaryRB != null)
        {
            mercenaryCC.enabled = true;
            mercenaryRB.isKinematic = false; // Make the Rigidbody non-kinematic to allow physics interactions
            mercenary.isMounted = false;
        }
        else
        {
            Debug.LogWarning($"{mercenary.gameObject.name} does not have a CharacterController component.");
        }

        if (mercenary.prevParent != null)
        {
            mercenary.gameObject.transform.SetParent(mercenary.prevParent.transform, true);
        }
        else
        {
            mercenary.gameObject.transform.SetParent(null);
        }
    }

    public void ApplyDriverInput(Vector2 input)
    {
        float forwardInput = Mathf.Max(0f, input.x);

        Vector3 movement =
            Vector3.right * forwardInput * moveSpeed * Time.deltaTime;

        transform.position += movement;

        // Only spin the wheels while actually moving; no input means no Rotate call, so rotation holds in place.
        if (forwardInput > 0f)
        {
            wheelAnimationTimer += Time.deltaTime;
            float frameInterval = 1f / Mathf.Max(1f, wheelAnimationFPS);

            // Step the rotation only once per simulated animation frame instead of every render frame.
            if (wheelAnimationTimer >= frameInterval)
            {
                float rotationAmount = forwardInput * wheelRotationSpeed * frameInterval;
                rodaKanan?.transform.Rotate(wheelRotationAxis, rotationAmount);
                rodaKiri?.transform.Rotate(wheelRotationAxis, rotationAmount);
                wheelAnimationTimer -= frameInterval;
            }
        }
    }

    private void OnWagonDestroyed()
    {
        if (isDestroyed) return; // Prevent multiple destruction calls
        isDestroyed = true;
        // TODO - add wagon destruction logic here (e.g., play destruction animation, disable wagon, etc.)
        Debug.Log($"{gameObject.name} has been destroyed!");
        DismountAllPlayers();
        animator.SetTrigger("OnDestroyed");
    }

    private void DismountAllPlayers()
    {
        if (isMechanicMounted && mechanic != null)
        {
            OnMechanicDismount();
        }

        if (isMercenaryMounted && mercenary != null)
        {
            OnMercenaryDismount();
        }
    }

    private void SetWagonState(WagonState newState)
    {
        if (currentWagonState == newState)
        {
            return;
        }

        currentWagonState = newState;
        UpdateWagonVisuals();
        UpdateWagonStateAnimation();
    }

    private void UpdateWagonVisuals()
    {
        bool showBroken = currentWagonState == WagonState.Broken && !isRepairingFunctionalHP;

        if (wagonFunctionalVisual != null)
        {
            wagonFunctionalVisual.SetActive(!showBroken);
        }

        if (wagonBrokenVisual != null)
        {
            wagonBrokenVisual.SetActive(showBroken);
        }

        if (repairIcon != null)
        {
            repairIcon.SetActive(currentWagonState == WagonState.Broken);
        }
    }

    private void UpdateWagonStateAnimation()
    {
        if (animator == null)
        {
            return;
        }

        switch (currentWagonState)
        {
            case WagonState.Functional:
                animator.SetTrigger("OnFunctional");
                break;
            case WagonState.Broken:
                animator.SetTrigger("OnBroken");
                break;
            default:
                Debug.LogWarning($"{gameObject.name} has an unknown wagon state.");
                break;
        }
    }

    private void UpdateHPUI()
    {
        if (wagonHPCanvas == null)
        {
            Debug.LogWarning($"{gameObject.name} is missing a wagon HP Canvas reference.");
            return;
        }

        bool repairingBrokenHP = IsRepairingBrokenHP();
        GameObject heartPrefab = repairingBrokenHP
            ? heartBrokenPrefab
            : heartFunctionalPrefab;
        int displayedHP = repairingBrokenHP ? currHPBroken : currHPFunctional;

        if (heartPrefab == null)
        {
            Debug.LogWarning($"{gameObject.name} is missing the required wagon HP heart prefab.");
            return;
        }

        Transform uiRoot = wagonHPCanvas.transform;
        for (int i = uiRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(uiRoot.GetChild(i).gameObject);
        }

        for (int i = 0; i < displayedHP; i++)
        {
            Instantiate(heartPrefab, uiRoot);
        }
    }

    private void AnimateLostHearts(int lostHealth)
    {
        if (wagonHPCanvas == null)
        {
            Debug.LogWarning($"{gameObject.name} is missing a wagon HP Canvas reference.");
            return;
        }

        Transform uiRoot = wagonHPCanvas.transform;
        int heartsToAnimate = Mathf.Min(lostHealth, uiRoot.childCount);

        for (int i = 0; i < heartsToAnimate; i++)
        {
            uiRoot.GetChild(uiRoot.childCount - 1 - i).GetComponent<Heart>()?.PlayDepleteAnimation();
        }
    }

    private bool CanOperate()
    {
        return currentWagonState == WagonState.Functional
            && !isBroken
            && currHPFunctional > 0;
    }
    public bool NeedsRepair()
    {
        if (isDestroyed)
        {
            return false;
        }

        // Broken covers two repair phases: filling currHPBroken, then currHPFunctional.
        return currentWagonState == WagonState.Broken
            ? currHPBroken < startHPBroken || currHPFunctional < startHPFunctional
            : currHPFunctional < startHPFunctional;
    }

    // Broken HP remains active until a repair fills that pool and advances to functional repair.
    private bool IsRepairingBrokenHP()
    {
        return currentWagonState == WagonState.Broken
            && !isRepairingFunctionalHP;
    }

    public void DestroyWagon(){Destroy(gameObject);}
    #endregion

    // Supposedly call by the destroy animation event
    
    #region Updates, Coroutines, and Gizmos
    private void Update()
    {
        if (!isMechanicMounted || mechanic == null || !CanOperate()) return;
        
        ApplyDriverInput(mechanic.GetMoveInput());
    }

    private IEnumerator IFrameCoroutine()
    {
        isIFrame = true;
        yield return new WaitForSeconds(IFrameDuration);
        isIFrame = false;
    }

    private IEnumerator HoldRepairRoutine(PlayerComponent interactor)
    {
        yield return new WaitForSeconds(repairEligibilityCheckDuration);

        if (pressingPlayer != interactor)
        {
            holdRepairRoutine = null;
            yield break;
        }

        if (!NeedsRepair() || PlayerInventory.Instance == null || PlayerInventory.Instance.repairMaterials < repairCost)
        {
            repairTriggeredThisPress = true;
            holdRepairRoutine = null;
            yield break;
        }

        SetRepairAnimation(interactor, true);
        yield return new WaitForSeconds(repairHoldThreshold);
        SetRepairAnimation(interactor, false);

        Debug.Log($"[WagonInteract] HoldRepairRoutine threshold reached at t={Time.time:F2}, pressingPlayer={(pressingPlayer ? pressingPlayer.gameObject.name : "null")}");

        if (pressingPlayer == interactor)
        {
            repairTriggeredThisPress = true;
            TryRepairFromInventory(interactor);
        }

        holdRepairRoutine = null;
    }

    private void SetRepairAnimation(PlayerComponent interactor, bool isRepairing)
    {
        if (interactor == null)
        {
            return;
        }

        interactor.isRepairing = isRepairing;
        interactor.animator?.SetBool("IsRepairing", isRepairing);
    }
    #endregion
}