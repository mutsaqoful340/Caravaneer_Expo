using UnityEngine;
using UnityEngine.ParticleSystemJobs;

public class VFX_ParticleInvoker : MonoBehaviour
{
    public ParticleSystem particle;

    public void OnPlayParticle()
    {
        particle.Play();
    }
}
