using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private void Start()
    {
        SFXManager.PostEvent("Start_BackgroundSubmarineMFX", gameObject);
    }

    private void OnEnable()
    {
        GameEventChannel<OnDeath>.OnEventRaised += OnPlayerDeath;
    }

    private void OnDisable()
    {
        GameEventChannel<OnDeath>.OnEventRaised -= OnPlayerDeath;
    }
    
    private void CallMapGeneration() //Leave for future use, Random Map Generation.
    {
        
    }

    private void OnPlayerDeath(OnDeath ev)
    {
        switch (ev.TypeOfDeath)
        {
            case DeathType.OxygenDepravation:
                break;
            case DeathType.SubmarineSunk:
                break;
        }
        
    }
}
