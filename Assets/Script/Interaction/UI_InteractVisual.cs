using UnityEngine;
using UnityEngine.UI;

public enum InteractionType
{
    [Tooltip("Single interaction type.")]
    One, 
    [Tooltip("Dual interaction type.")]
    Two
}

public enum InteractorRole
{
    Mechanic,
    Mercenary
}

public class UI_InteractVisual : MonoBehaviour
{
    public InteractionType interactionType = InteractionType.One;
    public Camera mainCamera;
    public GameObject interactIconPivot;
    public GameObject interactHintIcon;
    public GameObject interactIcon1;
    public GameObject interactIcon2_1;
    public bool interactIcon2_1Active = false;
    public GameObject interactIcon2_2;
    public bool interactIcon2_2Active = false;
    public GameObject twoInteractCover;

    public Vector3 offset = new Vector3(0, 2f, 0); // Offset to position the visual above the object

    private Image interactIconImage;
    private Image interactIcon2_1Image;
    private Image interactIcon2_2Image;
    private int playerCount = 0;

    private void Start()
    {
        interactIconImage = interactIcon1.GetComponent<Image>();
        interactIcon2_1Image = interactIcon2_1.GetComponent<Image>();
        interactIcon2_2Image = interactIcon2_2.GetComponent<Image>();

        if (InteractionType.One == interactionType)
        {
            twoInteractCover.SetActive(false);
            interactHintIcon.SetActive(true);
            interactIcon1.SetActive(false);
            interactIcon2_1.SetActive(false);
            interactIcon2_2.SetActive(false);
        }
        else if (InteractionType.Two == interactionType)
        {
            twoInteractCover.SetActive(false);
            interactHintIcon.SetActive(true);
            interactIcon1.SetActive(false);
            interactIcon2_1.SetActive(false);
            interactIcon2_2.SetActive(false);
        }


        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            interactHintIcon.SetActive(true);
            interactIcon1.SetActive(false);
        }
    }

    public void OnActivateVisual()
    {
        if (interactionType == InteractionType.One)
        {
            interactHintIcon.SetActive(false);
            interactIcon1.SetActive(true);
        }
        else if (interactionType == InteractionType.Two)
        {
            interactIcon2_1Active = true;
            interactIcon2_2Active = true;
            interactHintIcon.SetActive(false);
            interactIcon2_1.SetActive(true);
            interactIcon2_2.SetActive(true);
        }
    }

    public void OnDeactivateVisual()
    {
        if (interactionType == InteractionType.One)
        {
            interactHintIcon.SetActive(true);
            interactIcon1.SetActive(false);
        }
        else if (interactionType == InteractionType.Two)
        {
            interactIcon2_1Active = false;
            interactIcon2_2Active = false;
            interactHintIcon.SetActive(true);
            interactIcon2_1.SetActive(false);
            interactIcon2_2.SetActive(false);
        }
    }

    public void SetHoldProgress(float progress)
    {
        if (interactIconImage != null)
        {
            interactIconImage.fillAmount = Mathf.Clamp01(progress);
        }
    }

    // Each role's fill is capped to half the circle; both reaching full visually completes the disc.
    public void SetHoldProgress(InteractorRole role, float progress)
    {
        float halfFill = Mathf.Clamp01(progress) * 0.5f;

        if (role == InteractorRole.Mechanic && interactIcon2_1Image != null)
        {
            interactIcon2_1Image.fillAmount = halfFill;
        }
        else if (role == InteractorRole.Mercenary && interactIcon2_2Image != null)
        {
            interactIcon2_2Image.fillAmount = halfFill;
        }
    }
    
    private void LateUpdate()
    {
        if (mainCamera != null && interactIconPivot && interactIcon1 != null && interactHintIcon)
        {
            Vector3 gameObjectDirection = mainCamera.transform.position - transform.position;
            gameObjectDirection.y = 0f;

            if (gameObjectDirection.sqrMagnitude > 0f)
            {
                transform.rotation = Quaternion.LookRotation(gameObjectDirection, Vector3.up);
            }

            Vector3 visualDirection = mainCamera.transform.position - interactIconPivot.transform.position;

            if (visualDirection.sqrMagnitude > 0f)
            {
                Quaternion visualFacingRotation = Quaternion.LookRotation(visualDirection, Vector3.up);
                Vector3 visualRotation = interactIconPivot.transform.eulerAngles;
                visualRotation.x = visualFacingRotation.eulerAngles.x;
                interactIconPivot.transform.eulerAngles = visualRotation;
            }
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerCount++;
            UpdateTwoInteractCover();
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerCount = Mathf.Max(0, playerCount - 1);
            UpdateTwoInteractCover();
        }
    }

    private void UpdateTwoInteractCover()
    {
        if (twoInteractCover && interactionType == InteractionType.Two)
        {
            twoInteractCover.SetActive(playerCount >= 1);
        }
    }
}
