using System.Collections;
using UnityEngine;

public class TankRechargeTerminal : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField] private float rechargeRatePerSecond = 20f;
    [SerializeField] private float energyConsumption = 5f;
    [SerializeField] private Transform dockPoint;
    
    private OxygenTank _dockedTank;
    private Coroutine _rechargeCoroutine;
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
        
        if (_rechargeCoroutine != null) StopCoroutine(_rechargeCoroutine);
        _rechargeCoroutine = StartCoroutine(RechargeCoroutine());
    }

    private void UndockTank(PlayerCharacter player)
    {
        if (_rechargeCoroutine != null)
        {
            StopCoroutine(_rechargeCoroutine);
            _rechargeCoroutine = null;
        }
        StopEnergyConsumption();
        
        _dockedTank.Interact(player);
        _dockedTank = null;
    }

    private IEnumerator RechargeCoroutine()
    {
        while (_dockedTank != null)
        {
            if (!_dockedTank.isFull)
            {
                StartEnergyConsumption();
                _dockedTank.Refill(rechargeRatePerSecond * Time.deltaTime);
            }
            else
            {
                StopEnergyConsumption();
            }
            yield return null;
        }
        StopEnergyConsumption();
    }

    private void StartEnergyConsumption()
    {
        if (_hasRegisteredEnergyConsumption)
        {
            return;
        }

        _hasRegisteredEnergyConsumption = true;
        GameEventChannel<OnEnergyConsumption>.RaiseEvent(new OnEnergyConsumption(energyConsumption, true));
        Log.Info($"[TankRechargeTerminal] Energy consumption registered: {energyConsumption}");
    }

    private void StopEnergyConsumption()
    {
        if (!_hasRegisteredEnergyConsumption)
        {
            return;
        }

        _hasRegisteredEnergyConsumption = false;
        GameEventChannel<OnEnergyConsumption>.RaiseEvent(new OnEnergyConsumption(energyConsumption, false));
        Log.Info($"[TankRechargeTerminal] Energy consumption relieved: {energyConsumption}");
    }
}
