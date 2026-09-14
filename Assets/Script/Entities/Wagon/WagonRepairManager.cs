/// <summary>
/// This script manages the repair functionality for the wagon.
/// </summary>
using System.Collections;
using UnityEngine;

public class WagonRepairManager : MonoBehaviour
{
    [Tooltip("Disabled in favor of the wagon's direct long-press repair interaction. Kept for reference/possible reuse.")]
    public bool isSystemEnabled = false;
    public float repairRadius = 5f; // The radius within which the wagon can be repaired
    public float repairInterval = 1f; // The interval at which the the material will be consumed for repair
    public WagonComponent wagonComponent; // Reference to the wagon component
    [Tooltip("The prefab for the visual representation of the repair material that can be collected.")]
    public GameObject repairMaterialVisualPrefab; // The prefab for the visual of repair material that can be collected

    private Coroutine repairCoroutine;

    private void Start()
    {
        if (wagonComponent == null)
        {
            wagonComponent = WagonComponent.Instance;
        }
    }

    private void Update()
    {
        if (!isSystemEnabled)
        {
            return;
        }

        if (wagonComponent == null)
        {
            wagonComponent = WagonComponent.Instance;
        }

        bool canRepair = IsWithinRepairRadius() && !IsMechanicMounted();

        if (canRepair && repairCoroutine == null)
        {
            repairCoroutine = StartCoroutine(RepairWagonOverTime());
        }
        else if (!canRepair && repairCoroutine != null)
        {
            StopRepairCoroutine();
        }
    }

    private IEnumerator RepairWagonOverTime()
    {
        while (IsWithinRepairRadius() && !IsMechanicMounted())
        {
            yield return new WaitForSeconds(Mathf.Max(0.01f, repairInterval));

            if (!CanStartRepair())
            {
                break;
            }

            RepairMaterial materialPrefab = repairMaterialVisualPrefab.GetComponent<RepairMaterial>();
            if (!PlayerInventory.Instance.TrySpendRepairMaterials(materialPrefab.repairValue))
            {
                break;
            }

            GameObject visualObject = Instantiate(
                repairMaterialVisualPrefab,
                transform.position,
                Quaternion.identity);
            RepairMaterial visualMaterial = visualObject.GetComponent<RepairMaterial>();
            visualMaterial.isCollected = true;

            RepairMaterialVisual visual = visualObject.GetComponent<RepairMaterialVisual>();
            if (visual == null)
            {
                visual = visualObject.AddComponent<RepairMaterialVisual>();
            }

            visual.SetTarget(wagonComponent.transform);
        }

        repairCoroutine = null;
    }

    private bool CanStartRepair()
    {
        return wagonComponent != null
            && IsWithinRepairRadius()
            && !IsMechanicMounted()
            && wagonComponent.NeedsRepair()
            && PlayerInventory.Instance != null
            && repairMaterialVisualPrefab != null
            && repairMaterialVisualPrefab.GetComponent<RepairMaterial>() != null;
    }

    private bool IsMechanicMounted()
    {
        return wagonComponent != null && wagonComponent.isMechanicMounted;
    }

    private bool IsWithinRepairRadius()
    {
        return wagonComponent != null
            && (transform.position - wagonComponent.transform.position).sqrMagnitude
                <= repairRadius * repairRadius;
    }

    private void StopRepairCoroutine()
    {
        StopCoroutine(repairCoroutine);
        repairCoroutine = null;
    }

    private void OnDisable()
    {
        if (repairCoroutine != null)
        {
            StopRepairCoroutine();
        }
    }
    
}
