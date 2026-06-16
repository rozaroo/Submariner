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
        Log.Info("[GameManager] Reached Victory. Successful Evacuation.");
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        GameEventChannel<OnPlayerInputStateChanged>.RaiseEvent(new OnPlayerInputStateChanged(false));
        
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneWithFade("Win");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Win");
        }
    }
    
    private void CallMapGeneration()
    {
        
    }

    private void OnPlayerDeath(OnDeath ev)
    {
        Log.Info($"[GameManager] Jugador muerto por: {ev.TypeOfDeath}");
        switch (ev.TypeOfDeath)
        {
            case DeathType.OxygenDepravation:
                break;
            case DeathType.SubmarineSunk:
                break;
        }
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        GameEventChannel<OnPlayerInputStateChanged>.RaiseEvent(new OnPlayerInputStateChanged(false));

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneWithFade("Defeat");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Defeat");
        }
    }
}