using UnityEngine;

public class HUD_Information : MonoBehaviour
{
    public static HUD_Information Instance;
    public Animator animator;
    public bool isShowing;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        isShowing = false;
    }

    public void ShowInformation()
    {
        if (animator != null && !isShowing)
        {
            animator.SetTrigger("Open");
            isShowing = true;
        }
        else return;
    }

    public void ShowState()
    {
        isShowing = true;
    }

    public void HideState()
    {
        isShowing = false;
    }
}