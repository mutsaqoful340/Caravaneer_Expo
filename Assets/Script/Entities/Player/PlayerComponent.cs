using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public enum PlayerHPStage
{
    Alive,
    KnockedOut
}

[RequireComponent(typeof(PlayerInput), typeof(CharacterController))]
public class PlayerComponent : MonoBehaviour
{
    private static readonly string[] NormAttackTriggers = { "Attack1Norm", "Attack2Norm" };
    private static readonly string[] OppsAttackTriggers = { "Attack1Opps", "Attack2Opps" };

    [Header("Player Settings")]
    public PlayerHPStage currentHPStage = PlayerHPStage.Alive;
    [SerializeField] public bool isMercenary = false;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private int currHP = 3;
    [SerializeField] private int startHP = 3;
    [Tooltip("The range within which the player can search for enemy Game Objects.")]
    [SerializeField] private float searchRange = 2f;
    [Tooltip("The total cone angle in degrees within which the player can search for enemy Game Objects.")]
    [Range(0.1f, 360f)]
    [SerializeField] private float searchAngle = 180f;
    [Tooltip("Extra range added when checking if the currently tracked enemy is still valid, to avoid target flicker at the edge of searchRange.")]
    [SerializeField] private float trackedEnemyRangeBuffer = 0.5f;
    [SerializeField] private float knockOutDuration = 10f;
    public float IFrameDuration = 1f;
    [Tooltip("True for Mercenary, False for Driver")]
    [SerializeField] private float facingInputDeadZone = 0.1f;
    [SerializeField] private float groundY = 0f;
    [SerializeField] private float gravity = -18f;
    [Tooltip("The cooldown duration in seconds for the player's attack.")]
    public float attackCooldown = 1f; // Cooldown duration in seconds
    [Tooltip("The cooldown duration in seconds for resetting the attack sequence.") ]
    public float attackSequenceResetCooldown = 2f; // Cooldown duration in seconds

    [Header("Player Referenes")]
    public GameObject prevParent;
    public GameObject playerVisual;
    public Animator animator;
    public Sword sword;
    public Animator playerWeaponAnimator;
    [Tooltip("The UI element that displays the player's health.")]
    public GameObject playerStatUI;
    [Tooltip("The prefab for the heart icon representing health.")]
    public GameObject heartPrefab;
    public PlayerInput playerInputComp;
    [Tooltip("The inventory component that manages the player's collective items and resources.")]
    public PlayerInventory inventory;
    public PlayerReviveManager reviveManager;
    public GameObject reviveManagerPrefab;
    public Audio_Invoker audioInvoker;
    public GameObject Target => currentEnemy != null ? currentEnemy.gameObject : null;

    [Header("Debug")]
    [Tooltip("DO NOT ASSIGN THIS MANUALLY. Reference to the wagon currently in range, if any.")]
    [SerializeField] private WagonComponent wagonInteractObject;
    [Tooltip("DO NOT ASSIGN THIS MANUALLY. Reference to the interactable object currently in range, if any.")]
    [SerializeField] private EnemyComponent currentEnemy;
    [Tooltip("DO NOT ASSIGN THIS MANUALLY. This is a reference to the current enemy the player is interacting.")]
    [SerializeField] private Universal_Interact interactObject;
    // Locks the release event to whichever target actually received the press, so a target change mid-hold can't hijack the release.
    private enum PressedTarget { None, Generic, Wagon }
    private PressedTarget pressedTarget = PressedTarget.None;
    [Tooltip("DO NOT ASSIGN THIS MANUALLY. This is a reference to the current enemy the player is attacking.")]
    [SerializeField] private bool isAttacking = false;
    public bool isIFrame = false;
    public bool isMounted = false;
    [SerializeField] private bool isMoveOpposDir = false;
    public bool isRepairing = false;
    public bool IsTeleporting { get; set; }


    private Vector2 moveInput;
    private int currentAttackIndex;
    private float nextAttackTime;
    private float lastAttackTime = float.NegativeInfinity;
    private CharacterController characterController;
    private Vector3 verticalVelocity;
    // Release detection falls back to polling because the Input System's canceled message isn't reaching OnAction.
    private InputAction actionInput;
    private InputAction skipDialogueInput;
    private bool isSkipDialoguePressed;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (playerInputComp == null)
        {
            playerInputComp = GetComponent<PlayerInput>();
        }

        actionInput = playerInputComp?.actions?.FindAction("Action");
        skipDialogueInput = playerInputComp?.actions?.FindAction("SkipDialogue");

        prevParent = transform.parent != null ? transform.parent.gameObject : gameObject;
    }

    private void Start()
    {
        if (Manager_Input.Instance != null)
        {
            Manager_Input.Instance.RegisterPlayer(playerInputComp);
        }

        if (inventory == null)
        {
            inventory = PlayerInventory.Instance;
        }
        startHP = currHP; // Initialize startHP with the current HP value
        // OnUpdateHealthUI();

        if (!audioInvoker)
        {
            audioInvoker = GetComponent<Audio_Invoker>();
        }
    }

    // Reapplies starting HP after Start, so spawners can carry over upgrades bought before this player was instantiated.
    public void InitializeStartingHP(int hpStart)
    {
        startHP = hpStart;
        currHP = hpStart;
        RebuildHealthUI();
    }

    #region Input Callbacks
    public void OnMove(InputValue value)
    {
        if (currentHPStage == PlayerHPStage.KnockedOut) return;
        moveInput = value.Get<Vector2>();

        if (moveInput.x < -facingInputDeadZone)
        {
            isMoveOpposDir = true;
        }
        else if (moveInput.x > facingInputDeadZone)
        {
            isMoveOpposDir = false;
        }

        if (playerVisual != null && !isMounted)
        {
            Vector3 visualScale = playerVisual.transform.localScale;
            float scaleX = Mathf.Abs(visualScale.x);
            visualScale.x = isMoveOpposDir ? -scaleX : scaleX;
            playerVisual.transform.localScale = visualScale;
        }

        if (animator != null)
        {
            animator.SetBool("IsMoving", moveInput.sqrMagnitude > 0f);
        }
    }

    public void OnAction(InputValue value)
    {
        if (currentHPStage == PlayerHPStage.KnockedOut) return;
        Debug.Log($"[WagonInteract] OnAction invoked, isPressed={value.isPressed}, t={Time.time:F2}");

        RefreshInteractObjectReference();

        if (value.isPressed)
        {
            if (interactObject)
            {
                pressedTarget = PressedTarget.Generic;
                interactObject.BeginInteraction(this);
            }
            else if (wagonInteractObject)
            {
                pressedTarget = PressedTarget.Wagon;
                wagonInteractObject.BeginInteraction(this);
            }
            else if (reviveManager)
            {
                pressedTarget = PressedTarget.Generic; // Assuming reviveManager is treated as a generic interaction
                // reviveManager.OnRevivePlayer();
            }
            else
            {
                pressedTarget = PressedTarget.None;
            }

            OnPerformAction();

            Debug.Log($"{gameObject.name} performed an action!");
            return;
        }

        switch (pressedTarget)
        {
            case PressedTarget.Generic:
                interactObject?.EndInteraction(this);
                break;
            case PressedTarget.Wagon:
                Debug.Log($"[WagonInteract] {gameObject.name} released action at t={Time.time:F2}");
                wagonInteractObject?.EndInteraction(this);
                break;
        }

        pressedTarget = PressedTarget.None;
    }

    private void HandleActionReleasedByPolling()
    {
        if (pressedTarget == PressedTarget.None) return;

        Debug.Log($"[WagonInteract] {gameObject.name} release detected by polling at t={Time.time:F2}");

        switch (pressedTarget)
        {
            case PressedTarget.Generic:
                interactObject?.EndInteraction(this);
                break;
            case PressedTarget.Wagon:
                wagonInteractObject?.EndInteraction(this);
                break;
        }

        pressedTarget = PressedTarget.None;
    }

    public void OnPause(InputValue value)
    {
        if (value.isPressed)
        {
            if (Manager_Game.Instance.currentGameState == GameState.Gameplay)
            {
                Manager_Game.Instance.SetState(GameState.UI);
                Manager_UI.Instance.OnShowPanel("Panel1");
                return;
            }
        }
    }

    public void OnUINavigate(InputValue value)
    {
        if (Manager_Game.Instance.currentGameState == GameState.UI)
        {
            Vector2 navigationInput = value.Get<Vector2>();
            // Handle UI navigation logic here
        }
    }

    public void OnUISubmit(InputValue value)
    {
        if (Manager_Game.Instance.currentGameState == GameState.UI && value.isPressed)
        {
            // Handle UI submit logic here
            Debug.Log($"{gameObject.name} submitted UI action.");
        }
    }

    public void OnUICancel(InputValue value)
    {
        if (Manager_Game.Instance.currentGameState == GameState.UI && value.isPressed)
        {
            Debug.Log($"{gameObject.name} confirmed UI action.");
            
            if (UI_UnivConfirmPanel.Instance.isActive)
            {
                UI_UnivConfirmPanel.Instance.Cancel();
            }
        }
    }

    public void OnUIPause(InputValue value)
    {
        if (Manager_Game.Instance.currentGameState == GameState.UI && value.isPressed)
        {
            Debug.Log($"{gameObject.name} confirmed UI action.");
        }
    }

    public void OnSkipDialogue(InputValue value)
    {
        Debug.Log($"[VNDialogue] OnSkipDialogue invoked, isPressed={value.isPressed}, t={Time.time:F2}");
        if (Manager_Game.Instance.currentGameState != GameState.VN)
        {
            return;
        }

        if (value.isPressed)
        {
            isSkipDialoguePressed = true;
            VNDialogueSystem.Instance?.OnBeginSkipDialogue();
        }
        else
        {
            isSkipDialoguePressed = false;
            VNDialogueSystem.Instance?.OnEndSkipDialogue();
        }
    }
    #endregion

    #region Player Mechanics
    public void OnTriggerEnter(Collider other)
    {
        if (isMercenary && sword.isEnemySword) return; // Ignore if the sword is an enemy sword

        if (other.CompareTag("Wagon"))
        {
            WagonComponent wagon = other.GetComponent<WagonComponent>();
            if (wagon != null && wagonInteractObject == null)
            {
                wagonInteractObject = wagon;
            }
            return;
        }

        if (other.CompareTag("Interactable"))
        {
            Universal_Interact newInteractable = other.GetComponent<Universal_Interact>();

            if (newInteractable != null &&
                (interactObject == null || !interactObject.isActiveAndEnabled))
            {
                interactObject = newInteractable;
            }
        }

        if (other.CompareTag("ReviveManager"))
        {
            reviveManager = other.GetComponent<PlayerReviveManager>();
        }
    }
    
    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Wagon"))
        {
            if (wagonInteractObject != null && other.gameObject == wagonInteractObject.gameObject)
            {
                wagonInteractObject.EndInteraction(this);
                wagonInteractObject = null;
                if (pressedTarget == PressedTarget.Wagon) pressedTarget = PressedTarget.None;
            }
            return;
        }

        if (other.CompareTag("Interactable"))
        {
            if (interactObject != null && other.gameObject == interactObject.gameObject)
            {
                interactObject.EndInteraction();
                interactObject = null;
                if (pressedTarget == PressedTarget.Generic) pressedTarget = PressedTarget.None;
            }
        }

        if (other.CompareTag("ReviveManager"))
        {
            reviveManager = null;
        }
    }

    public void OnTakeDamage(int damage)
    {
        if (currentHPStage == PlayerHPStage.KnockedOut || damage <= 0 || isIFrame)
        {
            return;
        }

        StartCoroutine(IFrameCoroutine());
        CameraConstraint.Instance?.CameraShake();
        int previousHP = currHP;
        currHP -= damage;
        AddVerticalImpulse(1.5f);
        audioInvoker.OnPlaySFXLocal("ImpactFlesh", SFXType.OneShot);
        Debug.Log($"{gameObject.name} took {damage} damage! Remaining HP: {currHP}");
        AnimateLostHearts(previousHP - Mathf.Max(0, currHP));

        if (currHP <= 0)
        {
            if (currentHPStage == PlayerHPStage.Alive)
            {
                OnKnockOut();
            }
        }

        // Check after currentHPStage is finalized, otherwise a fresh knockout is missed
        Manager_GameLocal.Instance.OnCheckEntity();
    }

    private void OnKnockOut()
    {
        if (currentHPStage == PlayerHPStage.Alive)
        {
            currentHPStage = PlayerHPStage.KnockedOut;
            moveInput = Vector2.zero; // Stop stale directional input from sliding the player after knockout
            if (!isMercenary)
            {
                HUD_CharState.Instance?.SetCharacterState("Pia", "Knocked");
            }
            else
            {
                HUD_CharState.Instance?.SetCharacterState("Pippa", "Knocked");
            }
            GameObject reviveManagerObject = Instantiate(reviveManagerPrefab, transform.position, Quaternion.identity, transform);
            reviveManager = reviveManagerObject.GetComponent<PlayerReviveManager>();
            animator?.SetTrigger("Knocked");
            StartCoroutine(KnockOutCoroutine());
        }
        // TODO - add death logic here (e.g., play death animation, disable player controls, etc.)
        Debug.Log($"{gameObject.name} has died!");
    }

    private void OnDie()
    {
        if (currentHPStage == PlayerHPStage.KnockedOut)
        {
            animator?.SetTrigger("Die");
            Debug.Log($"{gameObject.name} has died!");
        }
    }

    public void OnRevive()
    {
        if (currentHPStage == PlayerHPStage.KnockedOut)
        {
            if (!isMercenary)
            {
                HUD_CharState.Instance?.SetCharacterState("Pia", "Revive");
            }
            else
            {
                HUD_CharState.Instance?.SetCharacterState("Pippa", "Revive");
            }
            currentHPStage = PlayerHPStage.Alive;
            currHP = startHP; // Revive with full health
            animator?.SetTrigger("Revive");
            RebuildHealthUI();
            Debug.Log($"{gameObject.name} has been revived!");
        }
    }

    public void AddVerticalImpulse(float amount)
    {
        verticalVelocity.y = Mathf.Max(verticalVelocity.y, amount);
    }

    private void RefreshInteractObjectReference()
    {
        if (!interactObject || !interactObject.isActiveAndEnabled)
        {
            interactObject = null;
        }

        if (!wagonInteractObject)
        {
            wagonInteractObject = null;
        }
    }

    private void AnimateLostHearts(int lostHealth)
    {
        if (playerStatUI == null || heartPrefab == null)
        {
            Debug.LogWarning($"{gameObject.name} is missing playerStatUI or heartPrefab reference.");
            return;
        }

        Transform uiRoot = playerStatUI.transform;
        int heartsToAnimate = Mathf.Min(lostHealth, uiRoot.childCount);

        for (int i = 0; i < heartsToAnimate; i++)
        {
            uiRoot.GetChild(uiRoot.childCount - 1 - i).GetComponent<Heart>()?.PlayDepleteAnimation();
        }

        Debug.Log($"{gameObject.name} has {currHP} HP remaining.");
    }

    private void RebuildHealthUI()
    {
        if (playerStatUI == null || heartPrefab == null)
        {
            Debug.LogWarning($"{gameObject.name} is missing playerStatUI or heartPrefab reference.");
            return;
        }

        Transform uiRoot = playerStatUI.transform;

        for (int i = uiRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(uiRoot.GetChild(i).gameObject);
        }

        int currentHealth = Mathf.Max(0, currHP);

        for (int i = 0; i < currentHealth; i++)
        {
            Instantiate(heartPrefab, uiRoot);
        }

        Debug.Log($"{gameObject.name} has {currHP} HP remaining.");
    }

    private void OnPerformAction()
    {
        if (!isMercenary || interactObject || reviveManager || Manager_Game.Instance.currentGameScene == GameScene.MainMenuScene) return;

        if (animator == null)
        {
            Debug.LogWarning($"{gameObject.name} is missing an Animator reference.");
            return;
        }

        if (Time.time < nextAttackTime) return;

        if (Time.time - lastAttackTime > attackSequenceResetCooldown)
        {
            currentAttackIndex = 0;
        }

        string triggerNorm = NormAttackTriggers[currentAttackIndex];
        string triggerOpps = OppsAttackTriggers[currentAttackIndex];
        if (!isMoveOpposDir)
        {
            playerWeaponAnimator.SetTrigger(triggerNorm);
            animator.SetTrigger(triggerNorm);
        }
        else
        {
            playerWeaponAnimator.SetTrigger(triggerOpps);
            animator.SetTrigger(triggerOpps);            
        }

        currentAttackIndex = (currentAttackIndex + 1) % NormAttackTriggers.Length;
        lastAttackTime = Time.time;
        nextAttackTime = Time.time + attackCooldown;

        Debug.Log($"{gameObject.name} performed mercenary combo step {currentAttackIndex}.");
    }

    private void OnSearchEnemy()
    {
        EnemyComponent previousEnemy = currentEnemy;
        currentEnemy = null;

        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, searchRange);
        Vector3 facingVector = new Vector3(isMoveOpposDir ? -1f : 1f, 0f, 0f);
        float halfSearchAngle = searchAngle * 0.5f;
        float closestDistanceSqr = float.PositiveInfinity;

        for (int i = 0; i < nearbyColliders.Length; i++)
        {
            EnemyComponent enemy = nearbyColliders[i].GetComponentInParent<EnemyComponent>();
            if (enemy == null || !enemy.isActiveAndEnabled)
            {
                continue;
            }

            Vector3 offset = enemy.transform.position - transform.position;
            offset.y = 0f;

            if (offset.sqrMagnitude <= Mathf.Epsilon || Vector3.Angle(facingVector, offset) > halfSearchAngle)
            {
                continue;
            }

            float distanceSqr = offset.sqrMagnitude;
            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                currentEnemy = enemy;
            }
        }

        // Keep the previous target when no replacement is found, even after the player turns away.
        if (currentEnemy == null && previousEnemy != null && previousEnemy.isActiveAndEnabled)
        {
            Vector3 previousOffset = previousEnemy.transform.position - transform.position;
            previousOffset.y = 0f;

            bool withinBufferedRange = previousOffset.sqrMagnitude <= (searchRange + trackedEnemyRangeBuffer) * (searchRange + trackedEnemyRangeBuffer);

            if (withinBufferedRange)
            {
                currentEnemy = previousEnemy;
            }
        }
    }

    public void OnAnimationPlaySFX(string sfxName)
    {
        audioInvoker.OnPlaySFXLocal(sfxName);
    }
    #endregion

    public Vector2 GetMoveInput()
    {
        return moveInput;
    }

    private void OnDestroy()
    {
        if (Manager_Input.Instance != null){
            Manager_Input.Instance.UnregisterPlayer(playerInputComp);}
    }

    #region Updates & Coroutines
    // Coroutines
    private IEnumerator KnockOutCoroutine()
    {
        yield return new WaitForSeconds(knockOutDuration);
        OnDie();
    }

    private IEnumerator IFrameCoroutine()
    {
        isIFrame = true;
        yield return new WaitForSeconds(IFrameDuration);
        isIFrame = false;
    }

    // Updates
    private void Update()
    {
        if (actionInput != null && pressedTarget != PressedTarget.None && !actionInput.IsPressed())
        {
            HandleActionReleasedByPolling();
        }

        if (skipDialogueInput != null && isSkipDialoguePressed && !skipDialogueInput.IsPressed())
        {
            isSkipDialoguePressed = false;
            VNDialogueSystem.Instance?.OnEndSkipDialogue();
        }

        if (isMercenary && currentHPStage == PlayerHPStage.Alive)
        {
            OnSearchEnemy();
        }

        if (isMounted) return;
        if (IsTeleporting) return;

        if (characterController.isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = 0f;
        }

        verticalVelocity.y += gravity * Time.deltaTime;

        Vector3 horizontalMovement = new Vector3(moveInput.x, 0f, moveInput.y) * moveSpeed;
        Vector3 movement = horizontalMovement * Time.deltaTime;
        movement.y = verticalVelocity.y * Time.deltaTime;

        characterController.Move(movement);

        if (transform.position.y < groundY)
        {
            Vector3 groundedPosition = transform.position;
            groundedPosition.y = groundY;
            transform.position = groundedPosition;
            verticalVelocity.y = 0f;
        }
    }

    // Gizmos
    private void OnDrawGizmos()
    {
        if (!isMercenary)
        {
            return;
        }

        Gizmos.color = Color.yellow;

        Vector3 center = transform.position;
        float facingDirection = isMoveOpposDir ? -1f : 1f;
        float halfSearchAngle = searchAngle * 0.5f;
        const int segmentCount = 24;
        Vector3 previousPoint = center + new Vector3(
            facingDirection * Mathf.Cos(-halfSearchAngle * Mathf.Deg2Rad) * searchRange,
            0f,
            Mathf.Sin(-halfSearchAngle * Mathf.Deg2Rad) * searchRange
        );

        for (int i = 1; i <= segmentCount; i++)
        {
            float angle = Mathf.Lerp(-halfSearchAngle, halfSearchAngle, i / (float)segmentCount) * Mathf.Deg2Rad;
            Vector3 currentPoint = center + new Vector3(
                facingDirection * Mathf.Cos(angle) * searchRange,
                0f,
                Mathf.Sin(angle) * searchRange
            );

            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }

        Gizmos.DrawLine(center, previousPoint);
        Gizmos.DrawLine(center, center + new Vector3(
            facingDirection * Mathf.Cos(-halfSearchAngle * Mathf.Deg2Rad) * searchRange,
            0f,
            Mathf.Sin(-halfSearchAngle * Mathf.Deg2Rad) * searchRange
        ));

        if (currentEnemy != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(center, currentEnemy.transform.position);
            Gizmos.DrawSphere(currentEnemy.transform.position, 0.15f);
        }
    }
    #endregion
}