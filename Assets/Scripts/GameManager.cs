using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private bool _isEnding;

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
        if (_isEnding) return;
        _isEnding = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneTransitionManager transition = SceneTransitionManager.Instance;
        if (transition != null && transition.isActiveAndEnabled)
        {
            transition.LoadSceneWithFade("Win");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Win");
        }

        HardModeSettings.UnlockAfterVictory();
        Log.Info("[GameManager] Reached Victory. Successful Evacuation.");
        GameEventChannel<OnPlayerInputStateChanged>.RaiseEvent(new OnPlayerInputStateChanged(false));
    }
    
    private void CallMapGeneration()
    {
        
    }

    private void OnPlayerDeath(OnDeath ev)
    {
        if (_isEnding) return;
        _isEnding = true;
        Log.Info($"[GameManager] Jugador muerto por: {ev.TypeOfDeath}");
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        GameEventChannel<OnPlayerInputStateChanged>.RaiseEvent(new OnPlayerInputStateChanged(false));

        if (SceneTransitionManager.Instance != null)
        {
            SFXManager.PostEvent("Stop_Tension_OnDeath_SFX", gameObject);
            if (ev.TypeOfDeath == DeathType.OxygenDepravation)
                SceneTransitionManager.Instance.LoadSceneAfterSuffocation("Defeat", FindFirstObjectByType<SuffocationEffect>());
            else
                SceneTransitionManager.Instance.LoadSceneWithFade("Defeat");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Defeat");
        }
    }
}
