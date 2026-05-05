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
        // Si hay un tanque en mano, colocarlo
        if (OxygenTank.CurrentHeld != null)
        {
            DockTank(OxygenTank.CurrentHeld);
            return;
        }

        // Si hay un tanque vacío en la terminal, el jugador lo retira
        if (_dockedTank != null && _dockedTank.IsEmpty)
            UndockTank(player);
    }

    private void DockTank(OxygenTank tank)
    {
        // Si ya había uno, lo sacamos primero
        if (_dockedTank != null) return;

        tank.Drop();
        _dockedTank = tank;

        // Colocar el tanque en el dock point visualmente
        tank.transform.SetParent(dockPoint);
        tank.transform.localPosition = Vector3.zero;
        tank.transform.localRotation = Quaternion.identity;

        Debug.Log("[OxygenTerminal] Tanque colocado. Cargando oxígeno...");
    }

    private void UndockTank(PlayerCharacter player)
    {
        _dockedTank.transform.SetParent(null);
        _dockedTank.Interact(player); // lo recoge el jugador directamente
        _dockedTank = null;
    }

    private void Update()
    {
        if (_dockedTank == null || _dockedTank.IsEmpty) return;

        float transferred = _dockedTank.Drain(transferRatePerSecond * Time.deltaTime);
        oxygenSystem.RestoreOxygen(transferred);

        if (_dockedTank.IsEmpty)
            Debug.Log("[OxygenTerminal] Tanque agotado. Reemplazarlo para continuar.");
    }
}
