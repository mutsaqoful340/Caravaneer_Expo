using UnityEngine;

public class Manager_Audio : MonoBehaviour
{
    public static Manager_Audio Instance {get; set;}

    [Header("BGM")]
    public AudioClip[] BGMOverworld;
    public AudioClip[] BGMBattle;
    public AudioClip[] BGMVN;
    public AudioClip[] BGMComic;

    [Header("=== SFX ===")]
    public AudioClip[] footSteps;
    public AudioClip[] swordSwings;
    public AudioClip[] enemySwordSwings;
    public AudioClip[] axeSwings;

    [Header("Impacts/Hits")]
    public AudioClip[] impactsWood;
    public AudioClip[] impactHammer;
    public AudioClip[] impactFlesh;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    #region Public Methods
    public AudioClip GetSFX(string sfxName)
    {
        switch (sfxName)
        {
            case "FootStep":
                return GetRandomClip(footSteps, sfxName);
            case "SwordSwing":
                return GetRandomClip(swordSwings, sfxName);
            case "EnemySwordSwing":
                return GetRandomClip(enemySwordSwings, sfxName);
            case "AxeSwing":
                return GetRandomClip(axeSwings, sfxName);
            case "ImpactWood":
                return GetRandomClip(impactsWood, sfxName);
            case "ImpactHammer":
                return GetRandomClip(impactHammer, sfxName);
            case "ImpactFlesh":
                return GetRandomClip(impactFlesh, sfxName);
            case "BGMOverworld":
                return GetRandomClip(BGMOverworld, sfxName);
            case "BGMBattle":
                return GetRandomClip(BGMBattle, sfxName);
            case "BGMVN":
                return GetRandomClip(BGMVN, sfxName);
            case "BGMComic":
                return GetRandomClip(BGMComic, sfxName);
            default:
                Debug.LogWarning($"Audio clip '{sfxName}' was not found.");
                return null;
        }
    }
    #endregion

    #region Helper Methods
    private AudioClip GetRandomClip(AudioClip[] clips, string sfxName)
    {
        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning($"Audio clip array '{sfxName}' is empty.");
            return null;
        }

        return clips[Random.Range(0, clips.Length)];
    }
    #endregion
}