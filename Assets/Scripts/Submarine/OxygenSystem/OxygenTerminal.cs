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
        player.InventorySystem.TryGetHeldItem(out OxygenTank oxygenTankItem);
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
        
        tank.Dock();
        _dockedTank = tank;
        
        tank.transform.position = dockPoint.position;
        tank.transform.rotation = dockPoint.rotation;
        _transferCoroutine = StartCoroutine(TransferCoroutine());
        Log.Info("[OxygenTerminal] Tank Docked.");
    }

    private void UndockTank(PlayerCharacter player)
    {
        if (_transferCoroutine != null)
        {
            StopCoroutine(_transferCoroutine);
            _transferCoroutine = null;
        }
        _dockedTank.Interact(player);
        _dockedTank = null;
        Log.Info("[OxygenTerminal] Tank Undocked.");
    }
    
    private IEnumerator TransferCoroutine()
    {
        yield return null;
        while (_dockedTank != null && !_dockedTank.isEmpty)
        {
            float toDrain = transferRatePerSecond * Time.deltaTime;
            float drained = _dockedTank.Drain(toDrain);
            oxygenSystem.RestoreOxygen(drained);
            
            if (oxygenSystem.CurrentOxygen >= oxygenSystem.MaxOxygen)
                yield break;

            yield return null;
        }
    }
}
