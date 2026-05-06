using UnityEngine;

// Presionar E con un tanque en mano para colocarlo.
// El tanque recarga el oxígeno del submarino hasta agotarse.
// Presionar E sobre el tanque vacío para retirarlo y reemplazarlo.
public class OxygenTerminal : MonoBehaviour, IInteractable
{
    [Header("Referencias")]
    [SerializeField] private OxygenSystem oxygenSystem;
    [SerializeField] private Transform dockPoint; // posición donde se coloca el tanque visualmente

    [Header("Configuración")]
    [SerializeField] private float transferRatePerSecond = 10f;

    private OxygenTank _dockedTank;

    public void Interact(PlayerCharacter player)
    {
        // Con tanque en mano: colocarlo
        if (OxygenTank.CurrentHeld != null)
        {
            DockTank(OxygenTank.CurrentHeld);
            return;
        }

        // Sin tanque en mano: retirar el que está en la terminal (vacío o no)
        if (_dockedTank != null)
            UndockTank(player);
    }

    private void DockTank(OxygenTank tank)
    {
        if (_dockedTank != null) return;

        tank.Dock();
        _dockedTank = tank;

        // Se posiciona en el dockPoint sin parentear para evitar deformación por escala
        tank.transform.position = dockPoint.position;
        tank.transform.rotation = dockPoint.rotation;

        Debug.Log("[OxygenTerminal] Tanque colocado.");
    }

    private void UndockTank(PlayerCharacter player)
    {
        _dockedTank.Interact(player);
        _dockedTank = null;
    }

    private void Update()
    {
        if (_dockedTank == null || _dockedTank.IsEmpty) return;
        if (oxygenSystem.CurrentOxygen >= oxygenSystem.MaxOxygen) return;

        float transferred = _dockedTank.Drain(transferRatePerSecond * Time.deltaTime);
        oxygenSystem.RestoreOxygen(transferred);

        if (_dockedTank.IsEmpty)
            Debug.Log("[OxygenTerminal] Tanque agotado. Reemplazarlo para continuar.");
    }
}
