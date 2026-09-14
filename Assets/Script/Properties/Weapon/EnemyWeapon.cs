using UnityEngine;

public class EnemyWeapon : MonoBehaviour
{
    public EnemyComponent enemyParent;
    public Sword enemySword;
    public float rotationSpeed = 5f;

    private void Start()
    {
        if (enemyParent == null)
        {
            enemyParent = GetComponentInParent<EnemyComponent>();
        }

        if (enemySword == null)
        {
            enemySword = GetComponentInChildren<Sword>();
        }
    }

    public void ResetDamaging()
    {
        if (enemySword != null)
        {
            enemySword.ResetDamaging();
        }
    }

    private void LateUpdate()
    {
        if (enemyParent != null && enemyParent.Target != null)
        {
            // Rotate the weapon to face the target
            Vector3 directionToTarget = enemyParent.Target.transform.position - transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
}
