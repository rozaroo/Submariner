using System.Collections;
using UnityEngine;

public class TankRechargeTerminal : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField] private float rechargeRatePerSecond = 20f;
    [SerializeField] private float energyConsumption = 5f;
    [SerializeField] private Transform dockPoint;
    [SerializeField] private EnergySystem energySystem;

    [Header("Interaction")]
    [SerializeField] private InteractionUIData interactionUIData;
    
    private OxygenTank _dockedTank;
    private Coroutine _rechargeCoroutine;
    private bool _hasRegisteredEnergyConsumption;
    private bool _isRechargePaused;

    private void OnEnable()
    {
        GameEventChannel<OnEnergyStatusChange>.OnEventRaised += OnEnergyStatusChanged;
    }

    private void OnDisable()
    {
        GameEventChannel<OnEnergyStatusChange>.OnEventRaised -= OnEnergyStatusChanged;
    }

    private void Start()
    {
        if (energySystem == null) return;
        energySystem.FuseBurned += PauseRecharge;
        energySystem.FuseRestored += ResumeRecharge;
    }

    private void OnDestroy()
    {
        if (energySystem == null) return;
        energySystem.FuseBurned -= PauseRecharge;
        energySystem.FuseRestored -= ResumeRecharge;
    }

    private void OnEnergyStatusChanged(OnEnergyStatusChange status)
    {
        if (status.energyStatus == EnergyStatus.Empty)
        {
            PauseRecharge();
            StopEnergyConsumption();
        }
        else
        {
            ResumeRecharge();
        }
    }

    public void Interact(PlayerCharacter player)
    {
        InventorySystem inventory = player.InventorySystem;
        if (_dockedTank != null)
        {
            if (inventory.TryPickUp(_dockedTank))
            {
                UndockTank();
            }
            return;
        }

        if (dockPoint != null && inventory.TryExtractHeldItem(out OxygenTank tank))
        {
            DockTank(tank);
        }
    }

    public InteractionFeedback SendInteractionFeedback(PlayerCharacter player)
    {
        if (_dockedTank != null)
        {
            return player?.InventorySystem?.HeldItem == null
                ? new InteractionFeedback("Release Oxygen Tank", interactionUIData)
                : new InteractionFeedback("Holding Another Item", interactionUIData, false);
        }

        if (dockPoint != null && player?.InventorySystem?.HeldItem is OxygenTank oxygenTank)
        {
            string text = oxygenTank.isFull ? "Place Oxygen Tank (Full)" : "Place Oxygen Tank";
            return new InteractionFeedback(text, interactionUIData);
        }

        return new InteractionFeedback("Requires Oxygen Tank", interactionUIData, false);
    }

    private void DockTank(OxygenTank tank)
    {
        _dockedTank = tank;
        _dockedTank.Dock();
        _dockedTank.transform.position = dockPoint.position;
        _dockedTank.transform.rotation = dockPoint.rotation;

        if (_rechargeCoroutine != null) StopCoroutine(_rechargeCoroutine);
        _rechargeCoroutine = StartCoroutine(RechargeCoroutine());
    }

    private void UndockTank()
    {
        if (_rechargeCoroutine != null)
        {
            StopCoroutine(_rechargeCoroutine);
            _rechargeCoroutine = null;
        }
        StopEnergyConsumption();

        _dockedTank = null;
    }

    private IEnumerator RechargeCoroutine()
    {
        while (_dockedTank != null)
        {
            if (_isRechargePaused)
            {
                StopEnergyConsumption();
                yield return null;
                continue;
            }
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
    }

    private void StartEnergyConsumption()
    {
        if (_hasRegisteredEnergyConsumption) return;
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

    public void PauseRecharge()
    {
        _isRechargePaused = true;
    }

    public void ResumeRecharge()
    {
        _isRechargePaused = false;
    }
}
