using UnityEngine;

public class HUD_Information : MonoBehaviour
{
    public static HUD_Information Instance;
    public Animator animator_Pia;
    public Animator animator_Pippa;
    public bool isShowing_Pia;
    public bool isShowing_Pippa;

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
        isShowing_Pia = false;
        isShowing_Pippa = false;
    }

    public void ShowInformation_Pia()
    {
        if (animator_Pia != null && !isShowing_Pia && !isShowing_Pippa)
        {
            animator_Pia.SetTrigger("Open");
            isShowing_Pia = true;
        }
        else return;
    }

    public void ShowInformation_Pippa()
    {
        if (animator_Pippa != null && !isShowing_Pippa && !isShowing_Pia)
        {
            animator_Pippa.SetTrigger("Open");
            isShowing_Pippa = true;
        }
        else return;
    }

    public void ShowState_Pia()
    {
        isShowing_Pia = true;
    }

    public void ShowState_Pippa()
    {
        isShowing_Pippa = true;
    }

    public void HideState_Pia()
    {
        isShowing_Pia = false;
    }

    public void HideState_Pippa()
    {
        isShowing_Pippa = false;
    }
}