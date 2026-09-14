using UnityEngine;
using System.Collections;

public class Audio_ContinuePlayer : MonoBehaviour
{
    public AudioSource SFX;
    public bool randomizePitch;
    public float minPitch = 0.95f;
    public float maxPitch = 1.05f;

    public void PlaySFX(AudioClip clip)
    {
        if (SFX == null)
        {
            Debug.LogWarning("Audio_WorldPlayer requires an AudioSource.");
            Destroy(gameObject);
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning("Audio_WorldPlayer received a null AudioClip.");
            Destroy(gameObject);
            return;
        }

        OnRandomPitch();
        SFX.PlayOneShot(clip);
        StartCoroutine(EnumDestroy(clip.length));
    }

    private void OnRandomPitch()
    {
        if (randomizePitch)
        {
            SFX.pitch = Random.Range(minPitch, maxPitch);
            return;
        }

        SFX.pitch = 1f;
    }

    private IEnumerator EnumDestroy(float destroyDelay)
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}
