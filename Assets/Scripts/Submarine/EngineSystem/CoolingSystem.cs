using System.Collections;
using UnityEngine;

public class CoolingSystem : MonoBehaviour
{
    [SerializeField] private EngineSystem engineSystem;

    [SerializeField] private float coolingInterval = 10f;
    [SerializeField] private float maxUsageTime = 30f;
    [SerializeField] private float cooldownTime = 20f;
    [SerializeField] private float energyConsumption = 5f; // Consumo ajustado a 5

    private CoolingState _state;
    private Coroutine _coolingRoutine;
    [SerializeField] private LeverPullStation coolingLeverPull;

    // Bandera de control similar a la de DrainageStation
    private bool _hasRegisteredCoolingConsumption;

    private void Start()
    {
        if (coolingLeverPull != null)
        {
            coolingLeverPull.onActivation += StartCooling;
            coolingLeverPull.onDeactivation += StopCooling;
        }
    }

    private void OnDestroy()
    {
        if (coolingLeverPull != null)
        {
            coolingLeverPull.onActivation -= StartCooling;
            coolingLeverPull.onDeactivation -= StopCooling;
        }
    }

    public void StartCooling()
    {
        Log.Info("Cooling Lever Activated.");
        if (_state != CoolingState.Off)
        {
            Log.Info("Cooling already active.");
            return;
        }

        if (!engineSystem.IsRunning())
        {
            Log.Info("Cooling blocked. Engine not running.");
            return;
        }

        _state = CoolingState.Active;
        AddEnergyConsumption();
        _coolingRoutine = StartCoroutine(CoolingRoutine());
        Log.Info("Cooling Started.");
    }

    public void StopCooling()
    {
        Log.Info("Cooling Stopped.");
        if (_state != CoolingState.Active) return;

        if (_coolingRoutine != null)
        {
            StopCoroutine(_coolingRoutine);
            _coolingRoutine = null;
        }

        RemoveEnergyConsumption();
        _state = CoolingState.Off;
    }

    private IEnumerator CoolingRoutine()
    {
        float elapsedTime = 0f;
        while (elapsedTime < maxUsageTime)
        {
            if (!engineSystem.IsRunning())
            {
                StopCooling();
                yield break;
            }
            Log.Info($"Cooling Tick | Elapsed: {elapsedTime}/{maxUsageTime}");
            yield return new WaitForSeconds(coolingInterval);
            engineSystem.CoolEngine();
            elapsedTime += coolingInterval;
        }
        Log.Info("Cooling applied.");
        EnterCooldown();
    }

    private void EnterCooldown()
    {
        Log.Info($"Cooling entered cooldown ({cooldownTime}s)");
        RemoveEnergyConsumption(); // Deja de consumir energía durante el cooldown
        _state = CoolingState.Cooldown;
        if (_coolingRoutine != null)
        {
            StopCoroutine(_coolingRoutine);
            _coolingRoutine = null;
        }
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        Log.Info("Cooling System Cooldown");
        yield return new WaitForSeconds(cooldownTime);
        _state = CoolingState.Off;
        Log.Info("Cooling cooldown finished.");
    }

    // Lógica de consumo de energía adaptada de DrainageStation
    private void AddEnergyConsumption()
    {
        if (_hasRegisteredCoolingConsumption)
        {
            return;
        }

        _hasRegisteredCoolingConsumption = true;
        GameEventChannel<OnEnergyConsumption>.RaiseEvent(new OnEnergyConsumption(energyConsumption, true));
        Log.Info($"Cooling consumption registered: {energyConsumption}");
    }

    private void RemoveEnergyConsumption()
    {
        if (!_hasRegisteredCoolingConsumption)
        {
            return;
        }

        _hasRegisteredCoolingConsumption = false;
        GameEventChannel<OnEnergyConsumption>.RaiseEvent(new OnEnergyConsumption(energyConsumption, false));
        Log.Info($"Cooling consumption relieved: {energyConsumption}");
    }
}