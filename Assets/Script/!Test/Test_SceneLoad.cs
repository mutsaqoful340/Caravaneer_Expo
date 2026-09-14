using UnityEngine;

public class Test_SceneLoad : MonoBehaviour
{
    public void OnLoadScene(string sceneName)
    {
        SceneLoader.Instance.LoadScene(sceneName);
    }
}