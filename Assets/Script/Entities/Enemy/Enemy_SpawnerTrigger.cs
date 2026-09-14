using UnityEngine;

public class Enemy_SpawnerTrigger : MonoBehaviour
{
    public Spawner_Enemy spawner;
    public bool isDestroyAfterSpawning;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Wagon"))
        {
            spawner.isSpawning = true;
            if (isDestroyAfterSpawning)
            {
                Destroy(gameObject);
            }
        }
    }
}