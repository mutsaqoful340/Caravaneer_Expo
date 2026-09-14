using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    public PlayerComponent playerParent;
    public float rotationSpeed = 5f;

    private void Start()
    {
        if (playerParent == null)
        {
            playerParent = GetComponentInParent<PlayerComponent>();
        }
    }

    private void LateUpdate()
    {
        if (playerParent != null && playerParent.Target != null)
        {
            Vector3 directionToTarget = playerParent.Target.transform.position - transform.position;
            directionToTarget.y = 0f;

            if (directionToTarget.sqrMagnitude > Mathf.Epsilon)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }
    }
}