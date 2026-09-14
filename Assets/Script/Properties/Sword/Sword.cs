using UnityEngine;
using System;

public class Sword : MonoBehaviour
{
    [Header("Sword Properties")]
    public int damage = 1;
    public bool isEnemySword = false; // Flag to determine if the sword belongs to an enemy

    [Header("Debug")]
    [SerializeField] private EnemyComponent enemyTarget;
    [SerializeField] private PlayerComponent playerTarget;
    [SerializeField] private WagonComponent wagonTarget;
    [SerializeField] private bool isDamaging = false;

    private void OnDisable()
    {
        enemyTarget = null;
        playerTarget = null;
        wagonTarget = null;
        isDamaging = false;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy") && !isEnemySword)
        {
            enemyTarget = other.GetComponent<EnemyComponent>();
            if (enemyTarget != null && !isDamaging)
            {
                isDamaging = true;
                enemyTarget.OnTakeDamage(damage, transform);
                Debug.Log($"{gameObject.name} dealt {damage} damage to {enemyTarget.gameObject.name}!");
            }
        }
        else if (other.CompareTag("Player") && isEnemySword)
        {
            playerTarget = other.GetComponent<PlayerComponent>();
            if (playerTarget != null && !isDamaging)
            {
                isDamaging = true;
                playerTarget.OnTakeDamage(damage);
                Debug.Log($"{gameObject.name} dealt {damage} damage to {playerTarget.gameObject.name}!");
            }
        }
        else if (other.CompareTag("Wagon") && isEnemySword)
        {
            wagonTarget = other.GetComponent<WagonComponent>();
            if (wagonTarget != null && !isDamaging)
            {
                isDamaging = true;
                wagonTarget.OnTakeDamage(damage);
                Debug.Log($"{gameObject.name} dealt {damage} damage to {wagonTarget.gameObject.name}!");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            enemyTarget = null;
            isDamaging = false;
        }
        else if (other.CompareTag("Player"))
        {
            playerTarget = null;
            isDamaging = false;
        }
        else if (other.CompareTag("Wagon"))
        {
            wagonTarget = null;
            isDamaging = false;
        }
    }

    public void ResetDamaging()
    {
        isDamaging = false;
    }
}
