using System;
using System.Collections;
using UnityEngine;

public class EngineSystem : MonoBehaviour
{ 
    [Header("Startup")] 
    [SerializeField] private float startupTime = 3f;
    [SerializeField] private LeverPullStation navigationLeverPull;

    [Header("Temperature")]
    [SerializeField] private float currentTemperature = 0f;
    [SerializeField] private float criticalTemperature = 85f;
    [SerializeField] private float maxTemperature = 100f;
    [SerializeField] private float temperatureIncreaseAmount = 1f;
    [SerializeField] private float temperatureIncreaseInterval = 5f;

    [Header("Cooling")]
    [SerializeField] private float coolingAmount = 2f;
    
    private EngineState _currentState = EngineState.Off;
    private Coroutine _temperatureCoroutine;
    
    public EngineState CurrentState => _currentState;

    #region UnityFunctions

    private void Start()
    {
        if (navigationLeverPull != null)
        {
            navigationLeverPull.onActivation += TryStartEngine;
            navigationLeverPull.onDeactivation += StopEngine;
        }
    }

    private void OnDestroy()
    {
        if (navigationLeverPull != null)
        {
            navigationLeverPull.onActivation -= TryStartEngine;
            navigationLeverPull.onDeactivation -= StopEngine;
        }
    }
    #endregion

    private void TryStartEngine()
    {
        if (_currentState != EngineState.Off) return;
        StartCoroutine(StartEngineRoutine());
    }

    private IEnumerator StartEngineRoutine()
    {
        _currentState = EngineState.Starting;

        yield return new WaitForSeconds(startupTime);

        _currentState = EngineState.Operative;
        Log.Info("Engine Started");
        _temperatureCoroutine = StartCoroutine(TemperatureRoutine());
        
        GameEventChannel<OnEngineStateChanged>.RaiseEvent(new OnEngineStateChanged { State = EngineState.Operative, SpeedMultiplier = 1f });
    }

    private IEnumerator TemperatureRoutine()
    {
        while (_currentState == EngineState.Operative || _currentState == EngineState.Degraded)
        {
            yield return new WaitForSeconds(temperatureIncreaseInterval);
            currentTemperature += temperatureIncreaseAmount;
            CheckTemperature();
        }
    }

    private void CheckTemperature()
    {
        if (currentTemperature >= maxTemperature)
        {
            BreakEngine();
            return;
        }
        if (currentTemperature >= criticalTemperature) EnterDegradedState();
    }

    private void EnterDegradedState()
    {
        if (_currentState == EngineState.Degraded) return;
        _currentState = EngineState.Degraded;
        
        GameEventChannel<OnEngineStateChanged>.RaiseEvent(new OnEngineStateChanged { State = EngineState.Degraded, SpeedMultiplier = 0.6f });
        Log.Info("Engine Degraded");
    }

    private void BreakEngine()
    {
        _currentState = EngineState.Broken;
        
        GameEventChannel<OnEngineStateChanged>.RaiseEvent(new OnEngineStateChanged { State = EngineState.Broken, SpeedMultiplier = 0f });

        if (_temperatureCoroutine != null)
        {
            StopCoroutine(_temperatureCoroutine);
            _temperatureCoroutine = null;
        }
        Log.Info("Engine Broken");
    }

    private void StopEngine()
    {
        _currentState = EngineState.Off;
        
        GameEventChannel<OnEngineStateChanged>.RaiseEvent(new OnEngineStateChanged { State = EngineState.Off, SpeedMultiplier = 0f });

        if (_temperatureCoroutine != null)
        {
            StopCoroutine(_temperatureCoroutine);
            _temperatureCoroutine = null;
        }
    }

    public void CoolEngine()
    {
        if (_currentState == EngineState.Off || _currentState == EngineState.Broken) return;
        currentTemperature -= coolingAmount;
        currentTemperature = Mathf.Max(0f, currentTemperature);
    }

    public bool CanBeCooled() => _currentState != EngineState.Off && _currentState != EngineState.Broken; //Not Used, but just in case.
    
    public bool IsRunning() => _currentState == EngineState.Operative || _currentState == EngineState.Degraded;

    public void RestartEngine()
    {
        if (_currentState != EngineState.Broken) return;
        currentTemperature = 0f;
        _currentState = EngineState.Off;
        if (navigationLeverPull != null) navigationLeverPull.SetActive(false);
        Log.Info("Engine Restarted");
    }
}