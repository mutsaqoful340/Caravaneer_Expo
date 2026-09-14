using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner_Enemy : MonoBehaviour
{
    public static Spawner_Enemy Instance { get; private set; }
    [Header("Enemy Spawner Settings")]
    public bool isMainSpawner = false;
    public Transform playerConstraint;
    [SerializeField] private GameObject[] enemyPrefab;
    [SerializeField] private float minSpawnRadius = 10f;
    [SerializeField] private float maxSpawnRadius = 20f;
    [SerializeField] private int enemiesToSpawnA = 3;
    [SerializeField] private int enemiesToSpawnB = 5;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int maxEnemyAlive = 1;
    [SerializeField] private LayerMask groundLayerMask;
    [SerializeField] private float spawnRayHeight = 50f;
    [SerializeField] private float spawnRayDistance = 200f;
    [SerializeField] private float enemySpawnHeightOffset = 0.5f;
    
    [Header("Enemy Settings")]
    public float minEnemyMoveSpeed = 3f;
    public float maxEnemyMoveSpeed = 5f;
    
    [Header("Spawn Mode Settings")]
    [Tooltip("If true, total enemies spawned are capped by poolSize, which decreases as enemies spawn.")]
    [SerializeField] private bool isSpawnFromPool = false;
    [Tooltip("Remaining enemy budget when isSpawnFromPool is true. Decreases as enemies spawn; spawning stops at 0.")]
    [SerializeField] private int poolSize = 10;

    [Header("Additional References")]
    public GameObject folder;

    [Header("Debug")]
    public bool isSpawning = true;
    [SerializeField] private GameObject[] enemies;
    [SerializeField] private Transform[] targetTransforms = new Transform[3];
    private bool targetReferencesReady;
    private float _spawnTimer;


    private void Awake()
    {
        // if (Instance != null && Instance != this)
        // {
        //     Destroy(gameObject);
        //     return;
        // }

        Instance = this;
    }

    // private void OnDestroy()
    // {
    //     if (Instance == this) Instance = null;
    // }

    private IEnumerator Start()
    {
        if (folder == null)
        {
            Debug.LogWarning("Spawner_Enemy: 'folder' is not assigned. Enemies will be spawned at root level.");
        }

        while (!TryGetTargetReferences())
        {
            yield return null;
        }

        if (WagonComponent.Instance != null && isMainSpawner)
        {
            playerConstraint = WagonComponent.Instance.transform;
        }
        NotifyEnemyPresence();
    }

    private void Update()
    {
        OnClearEnemyFromTheList();

        if (!isSpawning || !targetReferencesReady) return;

        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f)
        {
            OnSpawnEnemy();
            _spawnTimer = spawnInterval;
        }
    }

    private int _spawnCycle;

    private int GetEffectiveSpawnCount()
    {
        if (enemiesToSpawnA == 0 && enemiesToSpawnB == 0)
        {
            return 0;
        }

        if (enemiesToSpawnA == 0)
        {
            return enemiesToSpawnB;
        }

        if (enemiesToSpawnB == 0)
        {
            return enemiesToSpawnA;
        }

        int spawnCount = _spawnCycle % 2 == 0 ? enemiesToSpawnA : enemiesToSpawnB;
        _spawnCycle++;
        return spawnCount;
    }

    private void OnSpawnEnemy()
    {
        if (isSpawnFromPool && poolSize <= 0)
        {
            isSpawning = false;
            return;
        }

        int spawnCount = GetEffectiveSpawnCount();
        if (isSpawnFromPool)
        {
            spawnCount = Mathf.Min(spawnCount, poolSize);
        }

        SpawnEnemies(spawnCount, false);
    }

    private void SpawnEnemies(int requestedSpawnCount, bool ignoreAliveCap)
    {
        List<GameObject> spawnedEnemies = new List<GameObject>();
        int aliveEnemyCount = enemies == null ? 0 : enemies.Length;
        int availableSpawnSlots = maxEnemyAlive - aliveEnemyCount;
        int spawnCount = ignoreAliveCap ? requestedSpawnCount : Mathf.Min(requestedSpawnCount, availableSpawnSlots);

        if (spawnCount <= 0)
        {
            return;
        }

        if (enemyPrefab != null && folder != null)
        {
            Vector3 center = playerConstraint != null ? playerConstraint.transform.position : Vector3.zero;
            for (int i = 0; i < spawnCount; i++)
            {
                if (!TryGetSpawnPosition(center, out Vector3 spawnPos))
                {
                    continue;
                }

                GameObject prefab = enemyPrefab[Random.Range(0, enemyPrefab.Length)];
                GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity, folder.transform);
                EnemyComponent enemyComponent = enemy.GetComponent<EnemyComponent>();
                if (enemyComponent != null)
                {
                    enemyComponent.SetMoveSpeed(Random.Range(minEnemyMoveSpeed, maxEnemyMoveSpeed));
                    enemyComponent.SetPotentialTargets(targetTransforms);
                }

                spawnedEnemies.Add(enemy);
            }
        }
        else
        {
            Debug.LogWarning("Enemy prefab or spawn point is not assigned.");
        }

        OnAddEnemyToTheList(spawnedEnemies.ToArray());
    }

    private bool TryGetTargetReferences()
    {
        WagonComponent wagon = WagonComponent.Instance;
        PlayerComponent mechanic = null;
        PlayerComponent mercenary = null;

        PlayerComponent[] players = FindObjectsByType<PlayerComponent>(FindObjectsSortMode.None);
        foreach (PlayerComponent player in players)
        {
            if (player.gameObject.name.Contains("Player_Mechanic"))
            {
                mechanic = player;
            }
            else if (player.gameObject.name.Contains("Player_Mercenary"))
            {
                mercenary = player;
            }
        }

        if (wagon == null || mechanic == null || mercenary == null)
        {
            targetReferencesReady = false;
            return false;
        }

        targetTransforms[0] = wagon.transform;
        targetTransforms[1] = mechanic.transform;
        targetTransforms[2] = mercenary.transform;
        targetReferencesReady = true;
        return true;
    }

    private bool TryGetSpawnPosition(Vector3 center, out Vector3 spawnPos)
    {
        if (groundLayerMask.value == 0)
        {
            Debug.LogWarning("Spawner_Enemy: 'groundLayerMask' is not assigned. Consider setting the ground layer to prevent edge spawns.");
            spawnPos = center;
            return false;
        }

        for (int attempt = 0; attempt < 10; attempt++)
        {
            float radius = Random.Range(minSpawnRadius, maxSpawnRadius);
            Vector2 randomCircle = Random.insideUnitCircle.normalized * radius;
            Vector3 candidatePos = center + new Vector3(randomCircle.x, 0f, randomCircle.y);
            Vector3 rayOrigin = candidatePos + Vector3.up * spawnRayHeight;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, spawnRayDistance, groundLayerMask))
            {
                spawnPos = hit.point + Vector3.up * enemySpawnHeightOffset;
                return true;
            }
        }

        spawnPos = center;
        return false;
    }

    private void OnAddEnemyToTheList(GameObject[] newlySpawnedEnemies)
    {
        if (newlySpawnedEnemies == null || newlySpawnedEnemies.Length == 0)
        {
            return;
        }

        List<GameObject> trackedEnemies = enemies == null ? new List<GameObject>() : new List<GameObject>(enemies);
        trackedEnemies.AddRange(newlySpawnedEnemies);
        enemies = trackedEnemies.ToArray();

        if (isSpawnFromPool)
        {
            poolSize = Mathf.Max(0, poolSize - newlySpawnedEnemies.Length);
        }

        NotifyEnemyPresence();
    }

    private void OnClearEnemyFromTheList()
    {
        if (enemies == null || enemies.Length == 0)
        {
            return;
        }

        List<GameObject> aliveEnemies = new List<GameObject>(enemies.Length);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
            {
                aliveEnemies.Add(enemies[i]);
            }
        }

        if (aliveEnemies.Count != enemies.Length)
        {
            enemies = aliveEnemies.ToArray();
            NotifyEnemyPresence();
        }
    }

    private void NotifyEnemyPresence()
    {
        bool hasEnemies = enemies != null && enemies.Length > 0;
        CameraConstraint.Instance?.OnEnemyPresent(hasEnemies);
    }

    private void OnDrawGizmos()
    {
        Vector3 center = playerConstraint != null ? playerConstraint.position : transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, minSpawnRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, maxSpawnRadius);
    }
}