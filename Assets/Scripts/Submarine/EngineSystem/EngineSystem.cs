using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
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
    private Coroutine _engineStartingCoroutine;
    private Coroutine _temperatureCoroutine;

    #region UnityFunctions

    private void Start()
    {
        if (navigationLeverPull != null)
        {
            navigationLeverPull.onActivation += TryStartEngine;
            navigationLeverPull.onDeactivation += StopEngine;
        }
    }
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
        {
            currentTemperature = maxTemperature;

            Debug.Log($"[ENGINE TEST] Temperature forced to {currentTemperature}%");

            CheckTemperature();
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
        Log.Info($"[ENGINE] Start requested | Current State: {_currentState}");
        if (_currentState != EngineState.Off)
        {
            Log.Info("[ENGINE] Start denied. Engine is not in OFF state.");
            return;
        }
        _engineStartingCoroutine = StartCoroutine(StartEngineRoutine());
    }

    private IEnumerator StartEngineRoutine()
    {
        _currentState = EngineState.Starting;
        Log.Info($"[ENGINE] Status changed -> STARTING ({startupTime}s startup)");
        yield return new WaitForSeconds(startupTime);

        _currentState = EngineState.Operative;  
       Log.Info("[ENGINE] Status changed -> OPERATIVE"); SFXManager.PostEvent("Start_Motor_Engine", gameObject);
        _temperatureCoroutine = StartCoroutine(TemperatureRoutine());
        
        GameEventChannel<OnEngineStateChanged>.RaiseEvent(new OnEngineStateChanged { State = EngineState.Operative, SpeedMultiplier = 1f });
    }

    private IEnumerator TemperatureRoutine()
    {
        while (_currentState == EngineState.Operative || _currentState == EngineState.Degraded)
        {
            yield return new WaitForSeconds(temperatureIncreaseInterval);
            currentTemperature += temperatureIncreaseAmount;
            Log.Info($"Engine Temperature: {currentTemperature}%");
            CheckTemperature();
        }
    }

    private void CheckTemperature()
    {
        Log.Info($"[ENGINE] Temperature: {currentTemperature}%");
        if (currentTemperature >= maxTemperature)
        {
            Log.Info("[ENGINE] Maximum temperature reached.");
            BreakEngine();
            return;
        }
        if (currentTemperature >= criticalTemperature)
        {
            Log.Info("[ENGINE] Critical temperature reached.");
            EnterDegradedState();
        }
    }

    private void EnterDegradedState()
    {
        if (_currentState == EngineState.Degraded) return;
        _currentState = EngineState.Degraded;
        
        GameEventChannel<OnEngineStateChanged>.RaiseEvent(new OnEngineStateChanged { State = EngineState.Degraded, SpeedMultiplier = 0.6f });
        Log.Info("[ENGINE] Status changed -> DEGRADED");
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
        Log.Info("[ENGINE] Status changed -> BROKEN");
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
        if (_engineStartingCoroutine != null)
        {
            StopCoroutine(_engineStartingCoroutine);
            _engineStartingCoroutine = null;
        }
        
        SFXManager.PostEvent("Stop_Motor_Engine", gameObject);
        Log.Info("[ENGINE] Status changed -> OFF");
    }

    public void CoolEngine()
    {
        if (_currentState == EngineState.Off || _currentState == EngineState.Broken)
        {
            Log.Info("[ENGINE] Cooling ignored. Engine is OFF or BROKEN.");
            return;
        }
        currentTemperature -= coolingAmount;
        currentTemperature = Mathf.Max(0f, currentTemperature);
        Log.Info($"[ENGINE] Cooling active. Temperature: {currentTemperature}%");
    }

    public bool CanBeCooled() => _currentState != EngineState.Off && _currentState != EngineState.Broken; //Not Used, but just in case.
    
    public bool IsRunning() => _currentState == EngineState.Operative || _currentState == EngineState.Degraded;

    public void RestartEngine()
    {
        if (_currentState != EngineState.Broken) 
        {
            Log.Info("[ENGINE] Restart ignored. Engine is not broken.");
            return;
        }
        Log.Info("[ENGINE] Restart button pressed.");
        Log.Info("[ENGINE] Repairing engine...");
        currentTemperature = 0f;
        _currentState = EngineState.Off;
        Log.Info("[ENGINE] Temperature reset to 0%");
        Log.Info("[ENGINE] Status changed -> OFF");
        Log.Info("[ENGINE] Engine repaired. Pull the navigation lever to start it again.");
        if (_temperatureCoroutine != null) 
        {
            StopCoroutine(_temperatureCoroutine);
            _temperatureCoroutine = null;
        }
        if (_engineStartingCoroutine != null) 
        {
            StopCoroutine(_engineStartingCoroutine);
            _engineStartingCoroutine = null;
        }
        if (navigationLeverPull != null) navigationLeverPull.SetActive(false);
        GameEventChannel<OnEngineStateChanged>.RaiseEvent(new OnEngineStateChanged { State = EngineState.Off, SpeedMultiplier = 0f});
        SFXManager.PostEvent("Stop_Motor_Engine", gameObject);
        Log.Info("[ENGINE] Repair complete. Pull the navigation lever to start the engine.");
    }
}