using System.Collections;
using UnityEngine.AI;
using UnityEngine;

public enum EnemyType
{
    [Tooltip("Melee enemy type.")]
    Melee,
    [Tooltip("Ranged enemy type.")]
    Ranged
}

public class EnemyComponent : MonoBehaviour
{
    public EnemyType enemyType = EnemyType.Melee;
    [Header("Enemy Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private int HP = 3;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackUpwardForce = 3f;
    [SerializeField] private float knockbackDuration = 0.4f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackRange = 1.5f;

    [Header("Enemy References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private GameObject enemyVisual;
    public Animator weaponAnimator;
    public Audio_Invoker audioInvoker;
    [Tooltip("Only assign if enemy is RANGED.")]
    public GameObject axeSpawnPoint;
    [Tooltip("Only assign if enemy is RANGED.")]
    public GameObject axePrefab;

    [Header("Debug")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Sword playerSword;
    [SerializeField] private bool isDead = false;
    [SerializeField] private bool hasSpawnerMaterial = false;
    [SerializeField] private bool isMoving = false;
    private Coroutine knockbackRoutine;
    private Coroutine attackCooldownRoutine;
    private Quaternion rootRotation;

    public GameObject Target => targetTransform != null ? targetTransform.gameObject : null;
    private Vector3 spawnPosition;
    private Transform[] potentialTargets;

    private void Awake()
    {
        spawnPosition = transform.position;
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        rootRotation = transform.rotation;

        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.updateRotation = false;
        }

        // kinematic while navigating so gravity/physics never fights the NavMeshAgent
        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;

        if (agent != null)
        {
            agent.speed = moveSpeed;
        }
    }

    private void Start(){
        StartCoroutine(OnSpawnScale());

    }

    // === Enemy Actions ===
    #region Enemy Actions
    private void OnAttack()
    {
        if (isDead || targetTransform == null ||
            Vector3.Distance(transform.position, GetClosestPointOnTarget(targetTransform)) > attackRange)
        {
            return;
        }
        
        weaponAnimator.SetTrigger("Attack");
        animator.SetTrigger("Attack");
    }

    public void OnTakeDamage(int damage, Transform damageSource = null)
    {
        if (damage <= 0)
        {
            return;
        }

        CameraConstraint.Instance?.CameraShake();
        HP -= damage;

        Vector3 knockbackDirection = Vector3.zero;

        if (damageSource != null)
        {
            knockbackDirection = (transform.position - damageSource.position).normalized;
        }
        else if (playerSword != null)
        {
            knockbackDirection = (transform.position - playerSword.transform.position).normalized;
        }
        else if (targetTransform != null)
        {
            knockbackDirection = (transform.position - targetTransform.position).normalized;
        }

        audioInvoker.OnPlaySFXLocal("ImpactFlesh", SFXType.OneShot);
        OnKnockback(knockbackDirection);
        playerSword = null;

        if (HP <= 0)
        {
            if (!hasSpawnerMaterial)
            {
                hasSpawnerMaterial = true;
                Spawner_RepairMaterial.Instance?.SpawnRepairMaterial(transform.position);
            }
            else
            {
                Debug.LogWarning($"{gameObject.name} has no repair material to spawn.");
            }

            if (attackCooldownRoutine != null)
            {
                StopCoroutine(attackCooldownRoutine);
                attackCooldownRoutine = null;
            }

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            SetMoving(false);
            animator?.SetTrigger("Die");
            isDead = true;
            return;
        }
    }

    public void OnDie()
    {
        Destroy(gameObject);
    }

    public void OnDespawn()
    {
        Destroy(gameObject);
    }

    public void OnSpawnAxe()
    {
        if (axePrefab && axeSpawnPoint && enemyType == EnemyType.Ranged)
        {
            Instantiate(axePrefab, axeSpawnPoint.transform.position, axeSpawnPoint.transform.rotation);
        }
    }

    public void SetPotentialTargets(Transform[] targets)
    {
        potentialTargets = targets;
        targetTransform = GetClosestTargetFromSpawn();
    }

    private bool HasValidPlayerTarget()
    {
        if (potentialTargets == null)
        {
            return false;
        }

        foreach (Transform potentialTarget in potentialTargets)
        {
            if (potentialTarget == null)
            {
                continue;
            }

            PlayerComponent player = potentialTarget.GetComponent<PlayerComponent>();
            if (player != null && player.currentHPStage != PlayerHPStage.KnockedOut)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsValidTarget(Transform potentialTarget)
    {
        if (potentialTarget == null)
        {
            return false;
        }

        PlayerComponent player = potentialTarget.GetComponent<PlayerComponent>();
        return player == null || player.currentHPStage != PlayerHPStage.KnockedOut;
    }

    private void StopEnemyActions()
    {
        targetTransform = null;
        SetMoving(false);

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (attackCooldownRoutine != null)
        {
            StopCoroutine(attackCooldownRoutine);
            attackCooldownRoutine = null;
        }
    }

    private Transform GetClosestTargetFromSpawn()
    {
        if (potentialTargets == null)
        {
            return null;
        }

        Transform closestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Transform potentialTarget in potentialTargets)
        {
            if (!IsValidTarget(potentialTarget))
            {
                continue;
            }

            float distance = Vector3.Distance(spawnPosition, GetClosestPointOnTarget(potentialTarget));
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = potentialTarget;
            }
        }

        return closestTarget;
    }

    private Vector3 GetClosestPointOnTarget(Transform target)
    {
        Collider[] targetColliders = target.GetComponentsInChildren<Collider>();
        if (targetColliders.Length == 0)
        {
            return target.position;
        }

        Vector3 closestPoint = target.position;
        float closestDistance = Mathf.Infinity;

        foreach (Collider targetCollider in targetColliders)
        {
            if (targetCollider == null || !targetCollider.enabled)
            {
                continue;
            }

            Vector3 candidatePoint = targetCollider.ClosestPoint(transform.position);
            float candidateDistance = (transform.position - candidatePoint).sqrMagnitude;
            if (candidateDistance < closestDistance)
            {
                closestDistance = candidateDistance;
                closestPoint = candidatePoint;
            }
        }

        return closestPoint;
    }
    #endregion

    // === Knockback & Bounce ===
    #region Knockback & Bounce
    private void OnKnockback(Vector3 knockbackDirection)
    {
        if (knockbackDirection == Vector3.zero || rb == null) return;

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;
        }

        SetMoving(false);

        // hand movement over to physics so the Enemy physic material's bounciness can kick in
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 horizontalDirection = new Vector3(knockbackDirection.x, 0f, knockbackDirection.z).normalized;
        Vector3 knockbackImpulse = horizontalDirection * knockbackForce + Vector3.up * knockbackUpwardForce;
        rb.AddForce(knockbackImpulse, ForceMode.Impulse);

        if (knockbackRoutine != null)
        {
            StopCoroutine(knockbackRoutine);
        }

        if (!isDead)
        {
            knockbackRoutine = StartCoroutine(ResumeNavigationAfterKnockback());
        }
        else
        {
            knockbackRoutine = StartCoroutine(SettleAfterDeath());
        }
    }
    #endregion

    // === Updates & Coroutines ===
    #region Updates & Coroutines & Gizmos
    private void Update()
    {
        if (isDead)
        {
            return;
        }

        if (!HasValidPlayerTarget())
        {
            StopEnemyActions();
            return;
        }

        if (!IsValidTarget(targetTransform))
        {
            targetTransform = GetClosestTargetFromSpawn();
        }

        if (targetTransform != null)
        {
            transform.rotation = rootRotation;

            Vector3 targetPoint = GetClosestPointOnTarget(targetTransform);
            float distanceToTarget = Vector3.Distance(transform.position, targetPoint);
            bool isInAttackRange = distanceToTarget <= attackRange;

            if (isInAttackRange)
            {
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                }

                SetMoving(false);

                if (attackCooldownRoutine == null)
                {
                    attackCooldownRoutine = StartCoroutine(OnAttackCooldown());
                }
            }
            else if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(targetPoint);
                UpdateMovementAnimation();
            }

            UpdateFacing();
        }
    }

    private void SetMoving(bool moving)
    {
        isMoving = moving;
        animator?.SetBool("IsMoving", isMoving);
    }

    private void UpdateMovementAnimation()
    {
        bool agentIsMoving = agent != null &&
            agent.enabled &&
            agent.isOnNavMesh &&
            !agent.isStopped &&
            agent.desiredVelocity.sqrMagnitude > 0.01f;

        SetMoving(agentIsMoving);
    }

    private void UpdateFacing()
    {
        if (enemyVisual == null) return;

        float horizontalDirection = agent != null && agent.enabled
            ? agent.desiredVelocity.x
            : GetClosestPointOnTarget(targetTransform).x - transform.position.x;

        if (Mathf.Abs(horizontalDirection) <= 0.01f) return;

        Vector3 visualScale = enemyVisual.transform.localScale;
        float scaleX = Mathf.Abs(visualScale.x);
        visualScale.x = horizontalDirection < 0f ? -scaleX : scaleX;
        enemyVisual.transform.localScale = visualScale;
    }

    private IEnumerator OnSpawnScale(){
        float elapsedTime = 0f;
        float duration = 0.5f;
        Vector3 initialScale = Vector3.zero;
        Vector3 targetScale = Vector3.one;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            transform.localScale = Vector3.Lerp(initialScale, targetScale, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = targetScale;
    }

    private IEnumerator OnAttackCooldown()
    {
        yield return new WaitForSeconds(attackCooldown);
        attackCooldownRoutine = null;

        if (!isDead && targetTransform != null &&
            Vector3.Distance(transform.position, GetClosestPointOnTarget(targetTransform)) <= attackRange)
        {
            OnAttack();
        }
    }

    private IEnumerator SettleAfterDeath()
    {
        // let the death knockback bounce play out, then lock the corpse in place
        yield return new WaitForSeconds(knockbackDuration);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        knockbackRoutine = null;
    }

    private IEnumerator ResumeNavigationAfterKnockback()
    {
        // let physics bounce/settle for a fixed window before handing control back to the agent
        yield return new WaitForSeconds(knockbackDuration);

        if (isDead || agent == null)
        {
            knockbackRoutine = null;
            yield break;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            agent.enabled = true;
            agent.Warp(hit.position);
            agent.isStopped = false;
        }

        knockbackRoutine = null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, targetTransform != null ? targetTransform.position : transform.position);
    }
    #endregion
}