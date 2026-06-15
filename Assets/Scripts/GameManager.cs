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
        GameEventChannel<OnGameWon>.OnEventRaised += OnGameWon;
    }

    private void OnDisable()
    {
        GameEventChannel<OnDeath>.OnEventRaised -= OnPlayerDeath;
        GameEventChannel<OnGameWon>.OnEventRaised -= OnGameWon;
    }

    private void OnGameWon(OnGameWon gameWon)
    {
        
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
