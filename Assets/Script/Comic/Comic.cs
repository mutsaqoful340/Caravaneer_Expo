using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class Comic : MonoBehaviour
{
    public ComicPanel[] comicPanels;
    public Animator animator;
    public Image backrgoundSprite;
    public bool playOnStart;
    private int currentPanelIndex;
    private bool hasShown;
    public UnityEvent onComicOver;

    private void Start()
    {
        if (!animator)
        {
            animator = GetComponent<Animator>();
        }
        gameObject.SetActive(false);
        if (playOnStart)
        {
            gameObject.SetActive(true);
            OnPlayComic();
        }
    }

    public void OnPlayComic()
    {
        if (!hasShown)
        {
            animator.SetTrigger("Show");
            hasShown = true;
        }
        
        while (comicPanels != null && currentPanelIndex < comicPanels.Length)
        {
            ComicPanel comicPanel = comicPanels[currentPanelIndex];
            currentPanelIndex++;

            if (comicPanel)
            {
                comicPanel.OnShowPanel();
                return;
            }
        }
    }

    public void OnComicOver()
    {
        foreach (ComicPanel comicPanel in comicPanels)
        {
            if (comicPanel)
            {
                comicPanel.OnHidePanel();
                animator.SetTrigger("Hide");
            }
        }
        onComicOver?.Invoke();
    }

    public void OnComicDisable()
    {
        gameObject.SetActive(false);
    }
}