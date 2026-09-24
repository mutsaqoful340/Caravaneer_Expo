using UnityEngine;

public class HUD_Information_Pippa : MonoBehaviour
{
    public HUD_Information hudInformation;

    public void IsShowingTrue()
    {
        hudInformation.isShowing_Pippa = true;
    }

    public void IsShowingFalse()
    {
        hudInformation.isShowing_Pippa = false;
    }
}
