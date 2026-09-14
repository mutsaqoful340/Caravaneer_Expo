using UnityEngine;

public class Spawner_RepairMaterial : MonoBehaviour
{
    public static Spawner_RepairMaterial Instance { get; private set; }

    [Header("Repair Material Settings")]
    public GameObject repairMaterialPrefab;
    public float spawnYOffset = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Called by other scripts to spawn a repair material at their root position
    public void SpawnRepairMaterial(Vector3 position)
    {
        if (repairMaterialPrefab == null)
        {
            Debug.LogWarning("Spawner_RepairMaterial: 'repairMaterialPrefab' is not assigned.");
            return;
        }

        RepairMaterial repairMaterial = repairMaterialPrefab.GetComponent<RepairMaterial>();
        if (repairMaterial == null)
        {
            Debug.LogWarning("Spawner_RepairMaterial: 'repairMaterialPrefab' does not contain a RepairMaterial component.");
            return;
        }

        float spawnChance = Mathf.Clamp01(repairMaterial.spawnPercentage);
        if (Random.value > spawnChance)
        {
            Debug.Log($"Repair material did not spawn. Roll failed against spawn chance {spawnChance:P0}.");
            return;
        }

        Vector3 spawnPosition = position + Vector3.up * spawnYOffset;
        Instantiate(repairMaterialPrefab, spawnPosition, Quaternion.identity);
        Debug.Log($"Repair material spawned at {spawnPosition}.");
    }
}