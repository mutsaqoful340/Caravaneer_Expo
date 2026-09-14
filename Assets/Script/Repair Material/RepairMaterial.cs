using UnityEngine;

public class RepairMaterial : MonoBehaviour
{
    public int repairValue = 1; // The amount of repair this material provides
    [Range(0.1f, 1f)]
    public float spawnPercentage = 0.5f; // The chance of this material spawning when an enemy dies
    public bool isCollected = false; // Flag to check if the material has been collected

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isCollected)
        {
            PlayerComponent playerComponent = other.GetComponent<PlayerComponent>();
            if (playerComponent != null && !playerComponent.isMercenary)
            {
                isCollected = true; // Mark as collected to prevent multiple collections
                PlayerInventory.Instance.AddRepairMaterials(repairValue);
                Debug.Log($"Player collected a repair material. Total repair materials: {PlayerInventory.Instance.repairMaterials}");
                Destroy(gameObject); // Destroy the repair material after collection
            }
            else return;
        }
    }
}
