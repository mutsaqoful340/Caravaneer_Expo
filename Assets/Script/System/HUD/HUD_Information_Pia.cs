using UnityEngine;

public class HUD_Information_Pia : MonoBehaviour
{
    public HUD_Information hudInformation;

    public void IsShowingTrue()
    {
        hudInformation.isShowing_Pia = true;
    }

    public void IsShowingFalse()
    {
        hudInformation.isShowing_Pia = false;
    }
}