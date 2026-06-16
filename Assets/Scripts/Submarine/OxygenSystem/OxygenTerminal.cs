using System.Collections;
using UnityEngine;

public class OxygenTerminal : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private OxygenSystem oxygenSystem;
    [SerializeField] private Transform dockPoint;

    [Header("Settings")]
    [SerializeField] private float transferRatePerSecond = 10f;
    [SerializeField] private float energyConsumption = 5f;

    private OxygenTank _dockedTank;
    private Coroutine _transferCoroutine;
    private bool _hasRegisteredEnergyConsumption;

    public void Interact(PlayerCharacter player)
    {
        player.InventorySystem.TryExtractHeldItem(out OxygenTank oxygenTankItem);
        if (oxygenTankItem != null)
        {
            DockTank(oxygenTankItem);
            return;
        }

        if (_dockedTank != null)
        {
            UndockTank(player);
        }
    }

    private void DockTank(OxygenTank tank)
    {
        if (_dockedTank != null) return;
        
        _dockedTank = tank;
        _dockedTank.Dock();
        _dockedTank.transform.position = dockPoint.position;
        _dockedTank.transform.rotation = dockPoint.rotation;
        
        if (_transferCoroutine != null) StopCoroutine(_transferCoroutine);
        _transferCoroutine = StartCoroutine(TransferCoroutine());
        Log.Info("[OxygenTerminal] Tank Docked.");
    }

    private void UndockTank(PlayerCharacter player)
    {
        if (_transferCoroutine != null)
        {
            StopCoroutine(_transferCoroutine);
            _transferCoroutine = null;
            oxygenSystem.ResumeDrain();
        }
        StopEnergyConsumption();

        _dockedTank.Interact(player);
        _dockedTank = null;
        Log.Info("[OxygenTerminal] Tank Undocked.");
    }
    
    private IEnumerator TransferCoroutine()
    {
        yield return null;
        float accumulatedDrained = 0f;

        while (_dockedTank != null && !_dockedTank.isEmpty)
        {
            oxygenSystem.PauseDrain();
            StartEnergyConsumption();

            float toDrain = oxygenSystem.CurrentOxygen < oxygenSystem.MaxOxygen 
                ? transferRatePerSecond * Time.deltaTime 
                : Time.deltaTime; // Maintenance rate to match system drain

            float drained = _dockedTank.Drain(toDrain);
            accumulatedDrained += drained;
            
            if (accumulatedDrained >= 0.5f)
            {
                oxygenSystem.RestoreOxygen(accumulatedDrained);
                accumulatedDrained = 0f;
            }

            yield return null;
        }

        StopEnergyConsumption();
        if (accumulatedDrained > 0)
        {
            oxygenSystem.RestoreOxygen(accumulatedDrained);
        }

        oxygenSystem.ResumeDrain();
        _transferCoroutine = null;
    }

    private void StartEnergyConsumption()
    {
        if (_hasRegisteredEnergyConsumption)
        {
            return;
        }

        _hasRegisteredEnergyConsumption = true;
        GameEventChannel<OnEnergyConsumption>.RaiseEvent(new OnEnergyConsumption(energyConsumption, true));
        Log.Info($"[OxygenTerminal] Energy consumption registered: {energyConsumption}");
    }

    private void StopEnergyConsumption()
    {
        if (!_hasRegisteredEnergyConsumption)
        {
            return;
        }

        _hasRegisteredEnergyConsumption = false;
        GameEventChannel<OnEnergyConsumption>.RaiseEvent(new OnEnergyConsumption(energyConsumption, false));
        Log.Info($"[OxygenTerminal] Energy consumption relieved: {energyConsumption}");
    }
}
