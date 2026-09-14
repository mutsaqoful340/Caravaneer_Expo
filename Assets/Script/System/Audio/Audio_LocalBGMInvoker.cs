using UnityEngine;
using System.Collections.Generic;

public class Audio_LocalBGMInvoker : MonoBehaviour
{
    private static readonly List<Audio_LocalBGMInvoker> activeBGMInvokers = new List<Audio_LocalBGMInvoker>();

    public Audio_Invoker audioInvoker;
    [Range(0f, 1f)]
    public float bgmVolume = 1f;
    public string bgmName;

    private void OnEnable()
    {
        activeBGMInvokers.Add(this);
    }

    private void OnDisable()
    {
        activeBGMInvokers.Remove(this);
    }

    private void Start()
    {
        PlayBGM();
    }

    public void StopBGM()
    {
        if (audioInvoker != null)
        {
            audioInvoker.StopBGM();
        }
    }

    public void PlayBGM()
    {
        if (audioInvoker != null && !string.IsNullOrEmpty(bgmName))
        {
            StopAllBGM();
            audioInvoker.OnPlaySFXLocal(bgmName, SFXType.BGM, bgmVolume);
        }
    }

    public void StopAllBGM()
    {
        foreach (Audio_LocalBGMInvoker bgmInvoker in activeBGMInvokers)
        {
            bgmInvoker.StopBGM();
        }
    }
}
