using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class DrainageMinigame : MonoBehaviour
{
    [Header("Minigame References")]
    [SerializeField] private List<ButtonStation> buttons;
    
    [Header("Minigame Settings")]
    [SerializeField] private int buttonsToUnlock = 3;

    private int _playableButtonsCount;
    private int _buttonsPressedCount;
    
    public event Action FinishedMiniGame;
    public bool hasSucceeded { get; private set; }


    private void OnEnable()
    {
        foreach (var button in buttons)
        {
            if (button != null)
            {
                button.onActivation += OnAnyButtonHit;
            }
        }
    }

    private void OnDisable()
    {
        foreach (var button in buttons)
        {
            if (button != null)
            {
                button.onActivation -= OnAnyButtonHit;
            }
        }
    }
    
    public void SetupMiniGame()
    {
        RestartMinigame();
        SelectRandomButtons();
        CheckButtonAvailability();
    }
    
    #region MinigameLogic

    private void SelectRandomButtons()
    {
        List<ButtonStation> availableButtons = new List<ButtonStation>(buttons);
        _playableButtonsCount = 0;
        for (int i = 0; i < buttonsToUnlock && availableButtons.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, availableButtons.Count);
            availableButtons[randomIndex].Unlock();
            availableButtons.RemoveAt(randomIndex);
            _playableButtonsCount++;
        }
    }
    
    private void CheckButtonAvailability()
    {
        if (_playableButtonsCount == 0)
        {
            Log.Error("NOT ENOUGH BUTTONS IN THE SCENE FOR THE MINIGAME!");
            hasSucceeded = true;
            FinishedMiniGame?.Invoke();
        }
    }

    private void OnAnyButtonHit()
    {
        _buttonsPressedCount++;
        if (_buttonsPressedCount >= _playableButtonsCount)
        {
            OnSucceededMinigame();
        }
    }
    
    private void OnSucceededMinigame()
    {
        foreach (var button in buttons)
        {
            button.SetActive(true);
            hasSucceeded = true;
        }
        FinishedMiniGame?.Invoke();
    }
    
    public void RestartMinigame()
    {
        _buttonsPressedCount = 0;
        hasSucceeded = false;
        foreach (var button in buttons)
        {
            button.Restart();
        }
    }
    #endregion
}
