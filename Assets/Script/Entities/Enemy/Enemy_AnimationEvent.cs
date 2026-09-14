using UnityEngine;

public class Enemy_AnimationEvent : MonoBehaviour
{
    public EnemyComponent enemyComponent;

    public void OnSpawnAxe()
    {
        enemyComponent.OnSpawnAxe();
    }

    public void OnDie()
    {
        enemyComponent.OnDie();
    }
}