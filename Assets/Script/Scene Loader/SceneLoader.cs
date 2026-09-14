using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("Loading Settings")]
    [SerializeField, Min(0f)] private float minimumLoadingDuration = 5f;

    private bool isLoading;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void LoadScene(string sceneName)
    {
        if (isLoading)
        {
            Debug.LogWarning("SceneLoader_Advanced: A scene is already loading.");
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("SceneLoader_Advanced: Scene name is empty.");
            return;
        }

        StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        isLoading = true;
        UI_UnivLoading loadingScreen = UI_UnivLoading.Instance;

        if (loadingScreen != null)
        {
            yield return loadingScreen.ShowRoutine();
        }

        AsyncOperation asyncLoad;

        try
        {
            asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"SceneLoader_Advanced: Could not load scene '{sceneName}'. {exception.Message}");
            loadingScreen?.Hide();
            isLoading = false;
            yield break;
        }

        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            UI_UnivLoading.Instance?.SetProgress(progress);
            yield return null;
        }

        UI_UnivLoading.Instance?.SetProgress(0.99f);
        float bufferStartTime = Time.unscaledTime;

        while (Time.unscaledTime - bufferStartTime < minimumLoadingDuration)
        {
            yield return null;
        }

        UI_UnivLoading.Instance?.SetProgress(1f);

        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        if (loadingScreen != null)
        {
            yield return loadingScreen.HideRoutine();
        }

        isLoading = false;
    }
}