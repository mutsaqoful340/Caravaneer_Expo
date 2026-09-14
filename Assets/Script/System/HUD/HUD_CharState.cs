using UnityEngine;

public class HUD_CharState : MonoBehaviour
{
    public static HUD_CharState Instance { get; private set; }
    public Animator animatorPia;
    public Animator animatorPippa;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        
    }

    public void SetCharacterState(string characterName, string state)
    {
        if (characterName == "Pia")
        {
            animatorPia.SetTrigger(state);
        }
        else if (characterName == "Pippa")
        {
            animatorPippa.SetTrigger(state);
        }
    }
}