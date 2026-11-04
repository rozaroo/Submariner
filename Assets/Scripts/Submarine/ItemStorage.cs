using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class ItemStorage : MonoBehaviour, IInteractable
{
    private enum ToolType
    {
        OxygenTank,
        Blowtorch
    }

    [SerializeField] private ToolType toolType;
    [SerializeField] private Transform storagePoint;
    [SerializeField] private MonoBehaviour startingItem;
    [SerializeField] private InteractionUIData interactionUIData;

    private MonoBehaviour _storedItem;

    private string ItemName => toolType == ToolType.OxygenTank ? "Oxygen Tank" : "Blowtorch";

    private void Start()
    {
        if (storagePoint != null && startingItem is IPickable item && Accepts(item))
        {
            Store(item);
        }
    }

    public void Interact(PlayerCharacter player)
    {
        InventorySystem inventory = player.InventorySystem;
        if (_storedItem != null)
        {
            if (inventory.HeldItem != null) return;

            ((IInteractable)_storedItem).Interact(player);
            if (ReferenceEquals(inventory.HeldItem, _storedItem))
            {
                _storedItem = null;
            }
            return;
        }

        if (storagePoint != null && Accepts(inventory.HeldItem)
            && inventory.TryExtractHeldItem(out IPickable item))
        {
            Store(item);
        }
    }

    public InteractionFeedback SendInteractionFeedback(PlayerCharacter player)
    {
        IPickable heldItem = player?.InventorySystem?.HeldItem;
        if (_storedItem != null)
        {
            return heldItem == null
                ? new InteractionFeedback($"Take {ItemName}", interactionUIData)
                : new InteractionFeedback("Holding Another Item", interactionUIData, false);
        }

        if (storagePoint != null && Accepts(heldItem))
        {
            return new InteractionFeedback($"Store {ItemName}", interactionUIData);
        }

        return new InteractionFeedback("", interactionUIData, false);
    }

    private bool Accepts(IPickable item) =>
        toolType == ToolType.OxygenTank ? item is OxygenTank : item is Blowtorch;

    private void Store(IPickable item)
    {
        if (item is OxygenTank tank)
        {
            tank.Dock();
        }
        else if (item is Blowtorch blowtorch)
        {
            blowtorch.Dock();
        }

        _storedItem = (MonoBehaviour)item;
        Transform itemTransform = _storedItem.transform;
        itemTransform.SetParent(storagePoint, true);
        itemTransform.SetPositionAndRotation(storagePoint.position, storagePoint.rotation);
    }
}
