using UnityEngine;

// Colocar el tanque con E para recargarlo. Retirarlo con E sin nada en mano.
// Cuando el tanque está lleno se detiene solo.
public class TankRechargeTerminal : MonoBehaviour, IInteractable
{
    [Header("Configuración")]
    [SerializeField] private float rechargeRatePerSecond = 20f;
    [SerializeField] private Transform dockPoint;

    private OxygenTank _dockedTank;

    public void Interact(PlayerCharacter player)
    {
        if (OxygenTank.CurrentHeld != null)
        {
            DockTank(OxygenTank.CurrentHeld);
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

        Debug.Log("[TankRechargeTerminal] Tanque en recarga...");
    }

    private void UndockTank(PlayerCharacter player)
    {
        _dockedTank.Interact(player);
        _dockedTank = null;
    }

    private void Update()
    {
        if (_dockedTank == null || _dockedTank.IsFull) return;

        _dockedTank.Refill(rechargeRatePerSecond * Time.deltaTime);

        if (_dockedTank.IsFull)
            Debug.Log("[TankRechargeTerminal] Tanque cargado al máximo.");
    }
}
