using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnergyPanelInteractionZone : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private EnergyPanelControl energyPanelControl;

    public void Interact(PlayerCharacter player)
    {
        if (energyPanelControl == null)
        {
            Log.Warning("[EnergyPanelInteractionZone] Energy Panel Control Not Set");
            return;
        }

        energyPanelControl.Interact(player);
    }
}
