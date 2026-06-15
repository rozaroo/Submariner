using System.Collections;
using UnityEngine;

public class OxygenTerminal : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private OxygenSystem oxygenSystem;
    [SerializeField] private Transform dockPoint;

    [Header("Settings")]
    [SerializeField] private float transferRatePerSecond = 10f;

    private OxygenTank _dockedTank;
    private Coroutine _transferCoroutine;

    public void Interact(PlayerCharacter player)
    {
        player.InventorySystem.TryExtractHeldItem(out OxygenTank oxygenTankItem);
        if (oxygenTankItem != null)
        {
            DockTank(oxygenTankItem);
            return;
        }

        if (_dockedTank != null)
            UndockTank(player);
    }

    private void DockTank(OxygenTank tank)
    {
        if (_dockedTank != null) return;
        
        _dockedTank = tank;
        _dockedTank.Dock();
        _dockedTank.transform.position = dockPoint.position;
        _dockedTank.transform.rotation = dockPoint.rotation;
        
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
        _dockedTank.Interact(player);
        _dockedTank = null;
        Log.Info("[OxygenTerminal] Tank Undocked.");
    }
    
    private IEnumerator TransferCoroutine()
    {
        yield return null;
        oxygenSystem.PauseDrain();
        float accumulatedDrained = 0f;

        while (_dockedTank != null && !_dockedTank.isEmpty && oxygenSystem.CurrentOxygen < oxygenSystem.MaxOxygen)
        {
            float toDrain = transferRatePerSecond * Time.deltaTime;
            float drained = _dockedTank.Drain(toDrain);
            accumulatedDrained += drained;
            
            if (accumulatedDrained >= 0.5f)
            {
                oxygenSystem.RestoreOxygen(accumulatedDrained);
                accumulatedDrained = 0f;
            }

            yield return null;
        }

        if (accumulatedDrained > 0)
            oxygenSystem.RestoreOxygen(accumulatedDrained);

        oxygenSystem.ResumeDrain();
        _transferCoroutine = null;
    }
}
