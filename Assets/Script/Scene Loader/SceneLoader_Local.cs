using UnityEngine;

public class SceneLoader_Local : MonoBehaviour
{
    public void OnLoadScene(string sceneName)
    {
        SceneLoader.Instance.LoadScene(sceneName);
    }
}