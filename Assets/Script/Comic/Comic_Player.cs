using UnityEngine;

public class Comic_Player : MonoBehaviour
{
    public static Comic_Player Instance { set; get; }
    public Comic comicFirstPage;
    private void Awake()
    {
        Instance = this;
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