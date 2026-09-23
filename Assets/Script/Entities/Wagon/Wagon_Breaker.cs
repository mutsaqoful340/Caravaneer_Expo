using UnityEngine;

public class Wagon_Breaker : MonoBehaviour
{
    public WagonComponent wagon;
    private int functionalStartHP;
    private bool hasTriggered = false;

    public void GetWagonStats()
    {
        functionalStartHP = wagon.startHPFunctional;
    }

    public void BreakWagon()
    {
        if (wagon != null)
        {
            wagon.OnTakeDamage(functionalStartHP);
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Wagon"))
        {
            if (!hasTriggered)
            {
                BreakWagon();
                hasTriggered = true;
            }
        }
    }
}