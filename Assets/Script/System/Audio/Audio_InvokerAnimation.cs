using UnityEngine;

public class Audio_InvokerAnimation : MonoBehaviour
{
    public Audio_Invoker audioInvoker;

    public void OnPlaySFXOneshot(string sFXName)
    {
        audioInvoker.OnPlaySFXLocal(sFXName, SFXType.OneShot);
    }

    public void OnPlaySFXContinue(string sFXName)
    {
        audioInvoker.OnPlaySFXLocal(sFXName, SFXType.Continuous);
    }
}