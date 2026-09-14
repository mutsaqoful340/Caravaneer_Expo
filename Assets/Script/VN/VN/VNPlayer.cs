using UnityEngine;

public class VNPlayer : MonoBehaviour
{
    public static VNPlayer Instance {get; set;}
    public GameObject VN_OP_Prefab;
    public GameObject VN_ED_Prefab;
    public Animator buttonTutorial;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (VN_OP_Prefab)
        {
            Instantiate(VN_OP_Prefab, transform);
        }

        if (buttonTutorial)
        {
            buttonTutorial.gameObject.SetActive(false);
        }
    }

    public void OnPlayVN_ED()
    {
        if (VN_ED_Prefab)
        {
            Instantiate(VN_ED_Prefab, transform);
        }
    }

    public void OnPlayVN(GameObject vnPrefab)
    {
        if (vnPrefab)
        {
            Instantiate(vnPrefab, transform);
        }
    }

    public void ShowButtonTutorial()
    {
        if (buttonTutorial)
        {
            buttonTutorial.gameObject.SetActive(true);
            buttonTutorial.SetTrigger("Open");
        }
    }

    public void HideButtonTutorial()
    {
        if (buttonTutorial)
        {
            buttonTutorial.gameObject.SetActive(false);
            buttonTutorial.SetTrigger("Hide");
        }
    }
}