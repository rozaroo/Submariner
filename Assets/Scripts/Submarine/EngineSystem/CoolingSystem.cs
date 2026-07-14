using System.Collections;
using UnityEngine;

public class CoolingSystem : MonoBehaviour
{
    [SerializeField] private EngineSystem engineSystem;
    
    [SerializeField] private float coolingInterval = 10f;
    [SerializeField] private float maxUsageTime = 30f;
    [SerializeField] private float cooldownTime = 20f;
    [SerializeField] private float energyConsumption = 8f;

    private CoolingState _state;
    private Coroutine _coolingRoutine;
    [SerializeField] private LeverPullStation coolingLeverPull;

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
        if (_state != CoolingState.Off) return;
        if (!engineSystem.IsRunning()) return;
        _state = CoolingState.Active;
        AddEnergyConsumption();
        _coolingRoutine = StartCoroutine(CoolingRoutine());
    }

    public void StopCooling()
    {
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
            yield return new WaitForSeconds(coolingInterval);
            engineSystem.CoolEngine();
            elapsedTime += coolingInterval;
        }
        EnterCooldown();
    }

    private void EnterCooldown()
    {
        RemoveEnergyConsumption();
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
    }
    private void AddEnergyConsumption()
    {
        GameEventChannel<OnEnergyConsumption>.RaiseEvent(new OnEnergyConsumption{energyToConsumeRate = energyConsumption,isAddingStress = true});
    }
    private void RemoveEnergyConsumption()
    {
        GameEventChannel<OnEnergyConsumption>.RaiseEvent(new OnEnergyConsumption{energyToConsumeRate = energyConsumption,isAddingStress = false});
    }
}