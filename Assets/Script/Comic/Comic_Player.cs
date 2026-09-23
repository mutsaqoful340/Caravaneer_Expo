using UnityEngine;

public class Comic_Player : MonoBehaviour
{
    public static Comic_Player Instance { set; get; }
    public Comic comicFirstPage;
    public bool playOnStart = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (playOnStart)
        {
            OnPlayComic();
        }
    }

    public void OnPlayComic()
    {
        if (comicFirstPage)
        {
            comicFirstPage.gameObject.SetActive(true);
            comicFirstPage.OnPlayComic();
        }
    }
}