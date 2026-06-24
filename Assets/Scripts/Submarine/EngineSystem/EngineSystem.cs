using System.Collections;
using UnityEngine;

public class EngineSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SubmarineMovement submarineMovement;

    [Header("Startup")]
    [SerializeField] private float startupTime = 3f;

    private EngineState _currentState = EngineState.Off;

    public EngineState CurrentState => _currentState;
    [SerializeField] private LeverStation navigationLever;

    [Header("Temperature")]
    [SerializeField] private float currentTemperature = 0f;
    [SerializeField] private float criticalTemperature = 85f;
    [SerializeField] private float maxTemperature = 100f;
    [SerializeField] private float temperatureIncreaseAmount = 1f;
    [SerializeField] private float temperatureIncreaseInterval = 5f;
    private Coroutine _temperatureCoroutine;

    [Header("Cooling")]
    [SerializeField] private float coolingAmount = 2f;

    private void Start()
    {
        submarineMovement = FindFirstObjectByType<SubmarineMovement>();
        if (navigationLever != null)
        {
            navigationLever.onActivation += TryStartEngine;
            navigationLever.onDeactivation += StopEngine;
        }
    }

    private void OnDestroy()
    {
        if (navigationLever != null)
        {
            navigationLever.onActivation -= TryStartEngine;
            navigationLever.onDeactivation -= StopEngine;
        }
    }

    public void TryStartEngine()
    {
        if (_currentState != EngineState.Off) return;
        StartCoroutine(StartEngineRoutine());
    }

    private IEnumerator StartEngineRoutine()
    {
        _currentState = EngineState.Starting;

        yield return new WaitForSeconds(startupTime);

        _currentState = EngineState.Operative;
        _temperatureCoroutine = StartCoroutine(TemperatureRoutine());
        submarineMovement.SetSpeedMultiplier(1f);
        submarineMovement.StartMovingTowards();
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
        submarineMovement.SetSpeedMultiplier(0.6f);
        Log.Info("Engine Degraded");
    }
    private void BreakEngine()
    {
        _currentState = EngineState.Broken;
        submarineMovement.SetSpeedMultiplier(0f);
        submarineMovement.StopMovingTowards();

        if (_temperatureCoroutine != null)
        {
            StopCoroutine(_temperatureCoroutine);
            _temperatureCoroutine = null;
        }
        Log.Info("Engine Broken");
    }
    public void StopEngine()
    {
        _currentState = EngineState.Off;

        submarineMovement.SetSpeedMultiplier(0f);
        submarineMovement.StopMovingTowards();
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
    public bool CanBeCooled()
    {
        return _currentState != EngineState.Off && _currentState != EngineState.Broken;
    }
    public bool IsRunning()
    {
        return _currentState == EngineState.Operative || _currentState == EngineState.Degraded;
    }
    public void RestartEngine()
    {
        if (_currentState != EngineState.Broken) return;
        currentTemperature = 0f;
        _currentState = EngineState.Off;
        if (navigationLever != null) navigationLever.SetActive(false);
        Log.Info("Engine Restarted");
    }
}