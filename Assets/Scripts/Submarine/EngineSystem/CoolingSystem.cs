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

    public void StartCooling()
    {
        Debug.Log("Cooling Lever Activated.");
        if (_state != CoolingState.Off)
        {
            Debug.Log("Cooling already active.");
            return;
        }

        if (!engineSystem.IsRunning())
        {
            Debug.Log("Cooling blocked. Engine not running.");
            return;
        }
        _state = CoolingState.Active;
        AddEnergyConsumption();
        _coolingRoutine = StartCoroutine(CoolingRoutine());
        Debug.Log("Cooling Started.");
    }

    public void StopCooling()
    {
        Debug.Log("Cooling Stopped.");
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
            Debug.Log($"Cooling Tick | Elapsed: {elapsedTime}/{maxUsageTime}");
            yield return new WaitForSeconds(coolingInterval);
            engineSystem.CoolEngine();
            elapsedTime += coolingInterval;
        }
        Debug.Log("Cooling applied.");
        EnterCooldown();
    }

    private void EnterCooldown()
    {
        Debug.Log($"Cooling entered cooldown ({cooldownTime}s)");
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
        Debug.Log("Cooling System Cooldown");
        yield return new WaitForSeconds(cooldownTime);
        _state = CoolingState.Off;
        Debug.Log("Cooling cooldown finished.");
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