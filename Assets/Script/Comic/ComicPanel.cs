using UnityEngine;

public class ComicPanel : MonoBehaviour
{
    public Comic comicManager;
    public Animator animator;
    public bool hasShown;

    private void Start()
    {
        if (!animator)
        {
            animator = GetComponent<Animator>();
        }
    }

    public void OnShowPanel()
    {
        animator.ResetTrigger("Hide");
        animator.SetTrigger("Show");
    }

    public void OnHidePanel()
    {
        animator.ResetTrigger("Show");
        animator.SetTrigger("Hide");
    }

    public void OnComicAnimationComplete()
    {
        if (hasShown) return;

        hasShown = true;
        if (comicManager)
        {
            comicManager.OnPlayComic();
        }
    }

    public void OnComicOver()
    {
        if (comicManager)
        {
            comicManager.OnComicOver();
        }
    }
}