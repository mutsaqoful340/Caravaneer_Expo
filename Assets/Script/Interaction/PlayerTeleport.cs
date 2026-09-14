using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerTeleport : MonoBehaviour
{
    private const int RequiredPlayerCount = 2;

    [Header("Teleportation References")]
    [Tooltip("The start points of the SOURCE line that players will teleport from.")]
    public Transform sourceLineStart;
    [Tooltip("The end points of the SOURCE line that players will teleport from.")]
    public Transform sourceLineEnd;
    [Tooltip("The start points of the DESTINATION line that players will teleport to.")]
    public Transform destinationLineStart;
    [Tooltip("The end points of the DESTINATION line that players will teleport to.")]
    public Transform destinationLineEnd;
    [Tooltip("The paired gate that should ignore the incoming player until they leave its trigger volume.")]
    public PlayerTeleport pairedGate;

    [Header("Teleportation Settings")]
    [Tooltip("The time in seconds to ignore incoming players after they leave the trigger volume.")]
    [SerializeField, Min(0f)] private float incomingIgnoreFallbackSeconds = 0.25f;
    [SerializeField, Min(0f)] private float exitNudgeDistance = 0.1f;
    [SerializeField, Min(0f)] private float exitNudgeDuration = 0.12f;
    [Tooltip("The delay in seconds before teleporting players after both enter the trigger volume.")]
    [SerializeField, Min(0f)] private float teleportDelay = 0.05f;
    [Tooltip("Fixed world direction used for exit nudge. Keep Y at 0 for top-down movement.")]
    [SerializeField] private Vector3 exitNudgeWorldDirection = Vector3.left;

    [Header("Unity Events")]
    public UnityEvent onDelayStarted;
    public UnityEvent onPlayersTeleported;


    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private int playerCount = 0;
    [SerializeField] private PlayerComponent[] playerComponents = new PlayerComponent[RequiredPlayerCount];

    private readonly HashSet<int> playersInsideTrigger = new HashSet<int>();
    private readonly HashSet<int> playersPendingIgnore = new HashSet<int>();
    private readonly Dictionary<int, Coroutine> activeNudges = new Dictionary<int, Coroutine>();
    private readonly Dictionary<int, Vector3> pendingProjectionCompensation = new Dictionary<int, Vector3>();
    private readonly Dictionary<int, Collider> waitingPlayerColliders = new Dictionary<int, Collider>();
    private readonly Dictionary<int, int> playerColliderContacts = new Dictionary<int, int>();
    private Coroutine pendingTeleport;

    private void Awake()
    {
        EnsurePlayerSlots();
        RefreshWaitingPlayerCount();
        LogDebug($"Awake. Waiting players: {playerCount}/{RequiredPlayerCount}.");
    }

    private void OnValidate()
    {
        EnsurePlayerSlots();
        RefreshWaitingPlayerCount();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerComponent playerComponent = other.GetComponentInParent<PlayerComponent>();
        if (playerComponent == null)
        {
            LogDebug($"Trigger enter ignored: {other.name} has no PlayerComponent in its parents.");
            return;
        }

        int playerId = playerComponent.gameObject.GetInstanceID();

        if (playerColliderContacts.TryGetValue(playerId, out int contactCount))
        {
            playerColliderContacts[playerId] = contactCount + 1;
            LogDebug($"Additional collider entered for {playerComponent.name}. Contacts: {contactCount + 1}.");
            return;
        }

        playerColliderContacts[playerId] = 1;
        LogDebug($"Player entered: {playerComponent.name}. Contact registered.");

        if (playersPendingIgnore.Contains(playerId))
        {
            playersPendingIgnore.Remove(playerId);
            playersInsideTrigger.Add(playerId);
            LogDebug($"Incoming player accepted at paired gate: {playerComponent.name}.");
            return;
        }

        if (playersInsideTrigger.Contains(playerId))
        {
            LogDebug($"Duplicate trigger enter ignored for {playerComponent.name}.");
            return;
        }

        if (!AddWaitingPlayer(playerComponent, other))
        {
            LogDebug($"Could not add {playerComponent.name} to waiting players. Slots: {playerComponents.Length}, count: {playerCount}.");
            return;
        }

        playersInsideTrigger.Add(playerId);
        LogDebug($"Registered {playerComponent.name}. Waiting players: {playerCount}/{RequiredPlayerCount}.");

        if (playerCount == RequiredPlayerCount)
        {
            StartDelayedTeleport();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerComponent playerComponent = other.GetComponentInParent<PlayerComponent>();
        if (playerComponent == null)
        {
            return;
        }

        int playerId = playerComponent.gameObject.GetInstanceID();

        if (!playerColliderContacts.TryGetValue(playerId, out int contactCount))
        {
            LogDebug($"Trigger exit ignored for {playerComponent.name}: no matching contact was recorded.");
            return;
        }

        if (contactCount > 1)
        {
            playerColliderContacts[playerId] = contactCount - 1;
            LogDebug($"Collider exited for {playerComponent.name}. Remaining contacts: {contactCount - 1}.");
            return;
        }

        playerColliderContacts.Remove(playerId);
        playersInsideTrigger.Remove(playerId);
        playersPendingIgnore.Remove(playerId);
        RemoveWaitingPlayer(playerComponent);

        if (playerCount < RequiredPlayerCount)
        {
            CancelPendingTeleport();
        }

        LogDebug($"Player exited: {playerComponent.name}. Waiting players: {playerCount}/{RequiredPlayerCount}.");
    }

    private bool AddWaitingPlayer(PlayerComponent playerComponent, Collider playerCollider)
    {
        EnsurePlayerSlots();

        for (int i = 0; i < playerComponents.Length; i++)
        {
            if (playerComponents[i] == playerComponent)
            {
                return false;
            }
        }

        for (int i = 0; i < playerComponents.Length; i++)
        {
            if (playerComponents[i] != null)
            {
                continue;
            }

            playerComponents[i] = playerComponent;
            waitingPlayerColliders[playerComponent.gameObject.GetInstanceID()] = playerCollider;
            RefreshWaitingPlayerCount();
            LogDebug($"Added {playerComponent.name} to waiting slot {i}.");
            return true;
        }

        return false;
    }

    private void EnsurePlayerSlots()
    {
        if (playerComponents == null || playerComponents.Length != RequiredPlayerCount)
        {
            playerComponents = new PlayerComponent[RequiredPlayerCount];
            LogDebug($"Recreated player slots array with {RequiredPlayerCount} slots.");
        }
    }

    private void RemoveWaitingPlayer(PlayerComponent playerComponent)
    {
        for (int i = 0; i < playerComponents.Length; i++)
        {
            if (playerComponents[i] != playerComponent)
            {
                continue;
            }

            playerComponents[i] = null;
            waitingPlayerColliders.Remove(playerComponent.gameObject.GetInstanceID());
            RefreshWaitingPlayerCount();
            LogDebug($"Removed {playerComponent.name} from waiting slot {i}.");
            return;
        }
    }

    private void StartDelayedTeleport()
    {
        if (pendingTeleport != null)
        {
            LogDebug("Teleport delay is already running; no second delay started.");
            return;
        }

        if (teleportDelay <= 0f)
        {
            LogDebug("Both players are ready. Teleporting immediately because delay is zero.");
            TeleportWaitingPlayers();
            return;
        }

        onDelayStarted?.Invoke();
        LogDebug($"Both players are ready. Starting teleport delay: {teleportDelay:F3} seconds.");
        pendingTeleport = StartCoroutine(TeleportAfterDelay());
    }

    private IEnumerator TeleportAfterDelay()
    {
        yield return new WaitForSeconds(teleportDelay);
        pendingTeleport = null;
        LogDebug($"Teleport delay completed. Current waiting players: {playerCount}/{RequiredPlayerCount}.");

        if (playerCount == RequiredPlayerCount)
        {
            TeleportWaitingPlayers();
            onPlayersTeleported?.Invoke();
        }
        else
        {
            LogDebug("Teleport canceled after delay because two players are no longer waiting.");
        }
    }

    private void CancelPendingTeleport()
    {
        if (pendingTeleport == null)
        {
            return;
        }

        StopCoroutine(pendingTeleport);
        pendingTeleport = null;
        LogDebug($"Pending teleport canceled. Waiting players: {playerCount}/{RequiredPlayerCount}.");
    }

    private void TeleportWaitingPlayers()
    {
        if (!HasValidTeleportLines())
        {
            LogDebug("Teleport attempt stopped because teleport line validation failed.");
            return;
        }

        PlayerComponent[] waitingPlayers = new PlayerComponent[playerComponents.Length];
        playerComponents.CopyTo(waitingPlayers, 0);

        for (int i = 0; i < playerComponents.Length; i++)
        {
            playerComponents[i] = null;
        }

        RefreshWaitingPlayerCount();
        LogDebug($"Teleporting {waitingPlayers.Length} waiting player slots.");

        for (int i = 0; i < waitingPlayers.Length; i++)
        {
            PlayerComponent playerComponent = waitingPlayers[i];
            if (playerComponent == null)
            {
                continue;
            }

            int playerId = playerComponent.gameObject.GetInstanceID();
            waitingPlayerColliders.TryGetValue(playerId, out Collider playerCollider);
            waitingPlayerColliders.Remove(playerId);

            if (!TryTeleportPlayer(playerComponent.transform, playerCollider, playerId, out Vector3 appliedNudgeOffset))
            {
                LogDebug($"Teleport failed for {playerComponent.name}.");
                continue;
            }

            LogDebug($"Teleported {playerComponent.name}. Nudge offset: {appliedNudgeOffset}.");

            if (pairedGate != null)
            {
                pairedGate.QueueIncomingPlayer(playerId, appliedNudgeOffset);
                LogDebug($"Queued {playerComponent.name} at paired gate {pairedGate.name}.");
            }
        }
    }

    private bool HasValidTeleportLines()
    {
        if (sourceLineStart == null)
        {
            LogWarning("Cannot teleport: Source Line Start is not assigned.");
            return false;
        }

        if (sourceLineEnd == null)
        {
            LogWarning("Cannot teleport: Source Line End is not assigned.");
            return false;
        }

        if (destinationLineStart == null)
        {
            LogWarning("Cannot teleport: Destination Line Start is not assigned.");
            return false;
        }

        if (destinationLineEnd == null)
        {
            LogWarning("Cannot teleport: Destination Line End is not assigned.");
            return false;
        }

        if ((sourceLineEnd.position - sourceLineStart.position).sqrMagnitude <= Mathf.Epsilon)
        {
            LogWarning("Cannot teleport: Source line start and end are at the same position.");
            return false;
        }

        return true;
    }

    private void RefreshWaitingPlayerCount()
    {
        playerCount = 0;

        for (int i = 0; i < playerComponents.Length; i++)
        {
            if (playerComponents[i] != null)
            {
                playerCount++;
            }
        }
    }

    private void QueueIncomingPlayer(int playerId, Vector3 projectionCompensation)
    {
        playersPendingIgnore.Add(playerId);
        pendingProjectionCompensation[playerId] = projectionCompensation;
        LogDebug($"Queued incoming player ID {playerId} for paired-gate trigger ignore. Compensation: {projectionCompensation}.");
        StartCoroutine(ClearPendingIgnoreAfterDelay(playerId));
    }

    private IEnumerator ClearPendingIgnoreAfterDelay(int playerId)
    {
        if (incomingIgnoreFallbackSeconds > 0f)
        {
            yield return new WaitForSeconds(incomingIgnoreFallbackSeconds);
        }
        else
        {
            yield return null;
        }

        if (playersPendingIgnore.Contains(playerId) && !playersInsideTrigger.Contains(playerId))
        {
            playersPendingIgnore.Remove(playerId);
            pendingProjectionCompensation.Remove(playerId);
            LogDebug($"Cleared expired incoming-player ignore for ID {playerId}.");
        }
    }

    private bool TryTeleportPlayer(Transform playerTransform, Collider playerCollider, int playerId, out Vector3 appliedNudgeOffset)
    {
        appliedNudgeOffset = Vector3.zero;

        Vector3 sourceStart = sourceLineStart.position;
        Vector3 sourceEnd = sourceLineEnd.position;
        Vector3 destinationStart = destinationLineStart.position;
        Vector3 destinationEnd = destinationLineEnd.position;

        Vector3 sourceDirection = sourceEnd - sourceStart;
        float sourceLengthSqr = sourceDirection.sqrMagnitude;
        if (sourceLengthSqr <= Mathf.Epsilon)
        {
            LogWarning("Teleport failed: source line is too short.");
            return false;
        }

        float estimatedT = Vector3.Dot(playerTransform.position - sourceStart, sourceDirection) / sourceLengthSqr;
        estimatedT = Mathf.Clamp01(estimatedT);

        Vector3 closestPointOnSourceLine = sourceStart + sourceDirection * estimatedT;
        Vector3 projectionPosition = playerCollider != null ? playerCollider.ClosestPoint(closestPointOnSourceLine) : playerTransform.position;

        if (pendingProjectionCompensation.TryGetValue(playerId, out Vector3 compensation))
        {
            projectionPosition -= compensation;
            pendingProjectionCompensation.Remove(playerId);
        }

        float projectedT = Vector3.Dot(projectionPosition - sourceStart, sourceDirection) / sourceLengthSqr;
        projectedT = Mathf.Clamp01(projectedT);

        Vector3 destinationPosition = Vector3.Lerp(destinationStart, destinationEnd, projectedT);
        LogDebug($"Projection for player ID {playerId}: t={projectedT:F3}, destination={destinationPosition}.");

        Vector3 nudgeDirection = new Vector3(exitNudgeWorldDirection.x, 0f, exitNudgeWorldDirection.z);
        if (nudgeDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            nudgeDirection = Vector3.left;
        }
        else
        {
            nudgeDirection.Normalize();
        }

        destinationPosition.y = playerTransform.position.y;

        Rigidbody rigidbody = playerTransform.GetComponentInParent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = playerTransform.GetComponentInChildren<Rigidbody>();
        }
        Transform movementTransform = rigidbody != null ? rigidbody.transform : playerTransform.root;
        Vector3 movementOffset = movementTransform.position - playerTransform.position;

        LogDebug($"Moving {playerTransform.name}. Player position before: {playerTransform.position}, movement object: {movementTransform.name}, Rigidbody: {(rigidbody != null ? rigidbody.name : "none")}.");

        if (rigidbody != null)
        {
            Vector3 rigidbodyDestination = destinationPosition + movementOffset;
            rigidbody.position = rigidbodyDestination;
            rigidbody.transform.position = rigidbodyDestination;
            rigidbody.rotation = movementTransform.rotation;
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }
        else
        {
            movementTransform.SetPositionAndRotation(destinationPosition + movementOffset, movementTransform.rotation);
        }

        appliedNudgeOffset = exitNudgeDistance > 0f ? nudgeDirection * exitNudgeDistance : Vector3.zero;
        StartExitNudge(movementTransform, rigidbody, appliedNudgeOffset);
        Physics.SyncTransforms();
        LogDebug($"Applied teleport position to {playerTransform.name}: player position after: {playerTransform.position}, movement object position: {movementTransform.position}.");
        return true;
    }

    private void StartExitNudge(Transform movementTransform, Rigidbody rigidbody, Vector3 nudgeOffset)
    {
        if (nudgeOffset.sqrMagnitude <= Mathf.Epsilon || exitNudgeDuration <= 0f)
        {
            LogDebug($"Exit nudge skipped for {movementTransform.name}: distance or duration is zero.");
            return;
        }

        int playerId = movementTransform.root.gameObject.GetInstanceID();
        if (activeNudges.TryGetValue(playerId, out Coroutine runningNudge) && runningNudge != null)
        {
            StopCoroutine(runningNudge);
            LogDebug($"Stopped existing exit nudge for {movementTransform.name}.");
        }

        activeNudges[playerId] = StartCoroutine(ApplyExitNudge(movementTransform, rigidbody, nudgeOffset, playerId));
        LogDebug($"Started exit nudge for {movementTransform.name}: offset={nudgeOffset}, duration={exitNudgeDuration:F3} seconds.");
    }

    private IEnumerator ApplyExitNudge(Transform movementTransform, Rigidbody rigidbody, Vector3 nudgeOffset, int playerId)
    {
        yield return null;

        if (movementTransform == null)
        {
            activeNudges.Remove(playerId);
            yield break;
        }

        Vector3 startPosition = rigidbody != null ? rigidbody.position : movementTransform.position;
        Vector3 targetPosition = startPosition + nudgeOffset;

        float elapsed = 0f;
        while (elapsed < exitNudgeDuration)
        {
            if (movementTransform == null)
            {
                activeNudges.Remove(playerId);
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / exitNudgeDuration);
            float smoothT = t * t * (3f - 2f * t);
            Vector3 nudgedPosition = Vector3.Lerp(startPosition, targetPosition, smoothT);

            if (rigidbody != null)
            {
                rigidbody.position = nudgedPosition;
            }
            else
            {
                movementTransform.position = nudgedPosition;
            }

            yield return null;
        }

        if (movementTransform != null)
        {
            if (rigidbody != null)
            {
                rigidbody.position = targetPosition;
            }
            else
            {
                movementTransform.position = targetPosition;
            }
        }

        activeNudges.Remove(playerId);
        LogDebug($"Completed exit nudge for player ID {playerId}.");
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[{nameof(PlayerTeleport)}] {gameObject.name}: {message}", this);
        }
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[{nameof(PlayerTeleport)}] {gameObject.name}: {message}", this);
    }

    private void OnDrawGizmos()
    {
        if (sourceLineStart != null && sourceLineEnd != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(sourceLineStart.position, sourceLineEnd.position);
            Gizmos.DrawSphere(sourceLineStart.position, 0.1f);
            Gizmos.DrawSphere(sourceLineEnd.position, 0.1f); 
        }

        if (destinationLineStart != null && destinationLineEnd != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(destinationLineStart.position, destinationLineEnd.position);
            Gizmos.DrawSphere(destinationLineStart.position, 0.1f);
            Gizmos.DrawSphere(destinationLineEnd.position, 0.1f);
        }
    }
}