using UnityEngine;

public enum SFXType
{
    OneShot,
    Continuous,
    BGM
}
public class Audio_Invoker : MonoBehaviour
{
    public GameObject worldPlayerPrefab; // Will spawn the prefab at the caller's world transform
    public SFXType sFXType = SFXType.OneShot;
    private Audio_Player bgmPlayer;

    public void OnPlaySFXLocal(string sfxName)
    {
        OnPlaySFXLocal(sfxName, sFXType);
    }

    public void OnPlaySFXLocal(string sfxName, SFXType newSFXType)
    {
        OnPlaySFXLocal(sfxName, newSFXType, 1f);
    }

    public void OnPlaySFXLocal(string sfxName, SFXType newSFXType, float volume)
    {
        if (worldPlayerPrefab == null)
        {
            Debug.LogWarning("Audio_Invoker requires a world player prefab.");
            return;
        }

        if (Manager_Audio.Instance == null)
        {
            Debug.LogWarning("Cannot play SFX because Manager_Audio is missing.");
            return;
        }

        AudioClip clip = Manager_Audio.Instance.GetSFX(sfxName);
        if (clip == null)
        {
            return;
        }

        GameObject worldPlayerObject = Instantiate(
            worldPlayerPrefab,
            transform.position,
            transform.rotation);

        OnSFXType(newSFXType, worldPlayerObject, clip, volume);
    }

    public void OnSFXType(SFXType newSFXType, GameObject worldPlayerObject, AudioClip clip, float volume)
    {
        switch (newSFXType)
        {
            case SFXType.OneShot:
                if (worldPlayerObject.TryGetComponent<Audio_Player>(out Audio_Player oneShotPlayer))
                {
                    oneShotPlayer.PlaySFX(clip);
                    return;
                }
                break;
            case SFXType.Continuous:
                if (worldPlayerObject.TryGetComponent<Audio_Player>(out Audio_Player continuePlayer))
                {
                    worldPlayerObject.transform.SetParent(transform);
                    continuePlayer.PlayLoopingSFX(clip);
                    return;
                }
                break;
            case SFXType.BGM:
                if (worldPlayerObject.TryGetComponent<Audio_Player>(out Audio_Player newBgmPlayer))
                {
                    StopBGM();
                    worldPlayerObject.transform.SetParent(transform);
                    bgmPlayer = newBgmPlayer;
                    bgmPlayer.PlayLoopingSFX(clip);
                    bgmPlayer.SFX.volume = Mathf.Clamp01(volume);
                    return;
                }
                break;
            default:
                break;
        }

        Debug.LogWarning($"The world player prefab is missing an audio player for SFX type '{newSFXType}'.");
        Destroy(worldPlayerObject);
    }

    public void StopBGM()
    {
        if (bgmPlayer == null)
        {
            return;
        }

        bgmPlayer.StopSFX();
        Destroy(bgmPlayer.gameObject);
        bgmPlayer = null;
    }
}