using UnityEngine;

public class System_Bootsrap : MonoBehaviour
{
    [SerializeField] private string firstSceneName = "MainMenu";

    private void Start()
    {
        if (SceneLoader.Instance == null)
        {
            Debug.LogError("System_Bootsrap: No SceneLoader_Advanced exists in the Bootstrap scene.");
            return;
        }

        if (string.IsNullOrWhiteSpace(firstSceneName))
        {
            Debug.LogError("System_Bootsrap: First scene name is empty.");
            return;
        }

        SceneLoader.Instance.LoadScene(firstSceneName);
    }
}
