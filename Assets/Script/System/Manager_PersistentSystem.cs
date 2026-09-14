using UnityEngine;

public class Manager_PersistentSystem : MonoBehaviour
{
    private static Manager_PersistentSystem instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}