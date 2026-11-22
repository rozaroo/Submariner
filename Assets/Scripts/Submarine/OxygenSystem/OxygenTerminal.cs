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
    
    [Header("Interaction")]
    [SerializeField] private InteractionUIData interactionUIData;

    private OxygenTank _dockedTank;
    private Coroutine _transferCoroutine;
    private float _pendingOxygen;
    private bool _hasRegisteredEnergyConsumption;
    private bool _isBlackout;

    private void OnEnable()
    {
        GameEventChannel<OnEnergyStatusChange>.OnEventRaised += OnEnergyStatusChanged;
        if (_dockedTank != null && !_dockedTank.isEmpty && oxygenSystem != null)
            _transferCoroutine = StartCoroutine(TransferCoroutine());
    }

    private void OnDisable()
    {
        GameEventChannel<OnEnergyStatusChange>.OnEventRaised -= OnEnergyStatusChanged;
        if (_transferCoroutine == null) return;

        StopCoroutine(_transferCoroutine);
        FinishTransfer();
    }

    private void OnEnergyStatusChanged(OnEnergyStatusChange status)
    {
        _isBlackout = (status.energyStatus == EnergyStatus.Empty);
    }

    public void Interact(PlayerCharacter player)
    {
        InventorySystem inventory = player?.InventorySystem;
        if (inventory == null) return;

        if (_dockedTank != null)
        {
            if (!inventory.TryPickUp(_dockedTank)) return;

            _dockedTank = null;
            if (_transferCoroutine != null) StopCoroutine(_transferCoroutine);
            FinishTransfer();
            Log.Info("[OxygenTerminal] Tank Undocked.");
            return;
        }

        if (dockPoint != null && oxygenSystem != null && inventory.TryExtractHeldItem(out OxygenTank tank))
        {
            _dockedTank = tank;
            tank.Dock(dockPoint);
            _transferCoroutine = StartCoroutine(TransferCoroutine());
            Log.Info("[OxygenTerminal] Tank Docked.");
        }
    }

    public InteractionFeedback SendInteractionFeedback(PlayerCharacter player)
    {
        IPickable heldItem = player?.InventorySystem?.HeldItem;
        if (_dockedTank != null)
        {
            return heldItem == null
                ? new InteractionFeedback("Release Oxygen Tank", interactionUIData)
                : new InteractionFeedback("Holding Another Item", interactionUIData, false);
        }

        if (dockPoint != null && oxygenSystem != null && heldItem is OxygenTank oxygenTank)
        {
            string text = oxygenTank.isFull ? "Place Oxygen Tank (Full)" : "Place Oxygen Tank";
            return new InteractionFeedback(text, interactionUIData);
        }

        return new InteractionFeedback("Requires Oxygen Tank", interactionUIData, false);
    }

    private IEnumerator TransferCoroutine()
    {
        yield return null;

        while (_dockedTank != null && !_dockedTank.isEmpty)
        {
            if (_isBlackout)
            {
                StopEnergyConsumption();
                oxygenSystem.ResumeDrain();
                yield return null;
                continue;
            }

            oxygenSystem.PauseDrain();
            StartEnergyConsumption();

            float toDrain = oxygenSystem.CurrentOxygen < oxygenSystem.MaxOxygen
                ? transferRatePerSecond * Time.deltaTime
                : Time.deltaTime;

            _pendingOxygen += _dockedTank.Drain(toDrain);
            if (_pendingOxygen >= 0.5f) DeliverPendingOxygen();

            yield return null;
        }
        FinishTransfer();
    }

    private void FinishTransfer()
    {
        bool wasTransferring = _transferCoroutine != null;
        _transferCoroutine = null;
        StopEnergyConsumption();
        DeliverPendingOxygen();
        if (wasTransferring && oxygenSystem != null) oxygenSystem.ResumeDrain();
    }

    private void DeliverPendingOxygen()
    {
        float amount = _pendingOxygen;
        _pendingOxygen = 0f;
        if (amount > 0f && oxygenSystem != null) oxygenSystem.RestoreOxygen(amount);
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