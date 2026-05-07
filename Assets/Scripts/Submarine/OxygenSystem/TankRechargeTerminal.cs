using UnityEngine;

public class TankRechargeTerminal : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField] private float rechargeRatePerSecond = 20f;
    [SerializeField] private Transform dockPoint;
    private OxygenTank _dockedTank;

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
        
        _dockedTank.StartRefill(rechargeRatePerSecond);
    }

    private void UndockTank(PlayerCharacter player)
    {
        _dockedTank.StopRefill();
        _dockedTank.Interact(player);
        _dockedTank = null;
    }
}
