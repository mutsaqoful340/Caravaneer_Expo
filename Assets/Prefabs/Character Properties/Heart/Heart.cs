using UnityEngine;

public class Heart : MonoBehaviour
{
    public Animator animator;

    private void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    public void PlayDepleteAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Deplete");
        }
    }

    public void OnAnimationComplete()
    {
        Destroy(gameObject);
    }
}
