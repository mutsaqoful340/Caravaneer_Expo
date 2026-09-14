using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using System.Collections;

public class UI_UnivLoading : MonoBehaviour
{
    public static UI_UnivLoading Instance { get; private set; }

    [Header("Loading Screen Elements")]
    [SerializeField] private PlayableDirector loadingIN;
    [SerializeField] private PlayableDirector loadingOUT;
    [SerializeField] private Slider loadingProgressBar;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (loadingProgressBar != null)
        {
            loadingProgressBar.value = 0f;
            loadingProgressBar.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        Hide();
    }

    public void Show()
    {
        gameObject.SetActive(true);

        if (loadingProgressBar != null)
        {
            loadingProgressBar.value = 0f;
            loadingProgressBar.gameObject.SetActive(false);
        }
    }

    public IEnumerator ShowRoutine()
    {
        Show();
        yield return PlayTimelineAndWait(loadingIN);

        if (loadingProgressBar != null)
        {
            loadingProgressBar.gameObject.SetActive(true);
        }
    }

    public void SetProgress(float progress)
    {
        if (loadingProgressBar == null)
        {
            return;
        }

        loadingProgressBar.value = Mathf.Clamp01(progress);
    }

    public void Hide()
    {
        if (loadingProgressBar != null)
        {
            loadingProgressBar.gameObject.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    public IEnumerator HideRoutine()
    {
        if (loadingProgressBar != null)
        {
            loadingProgressBar.gameObject.SetActive(false);
        }

        yield return PlayTimelineAndWait(loadingOUT);
        gameObject.SetActive(false);
    }

    private IEnumerator PlayTimelineAndWait(PlayableDirector director)
    {
        if (director == null || director.playableAsset == null)
        {
            yield break;
        }

        director.Stop();
        director.time = 0d;
        director.Evaluate();
        director.Play();

        while (director.time < director.duration)
        {
            yield return null;
        }

        director.Stop();
    }
}