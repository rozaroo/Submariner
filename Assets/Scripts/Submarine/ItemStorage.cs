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
    [Tooltip("Optional starting tool. Place it within this support's collider in the scene.")]
    [SerializeField] private MonoBehaviour startingItem;
    [SerializeField] private InteractionUIData interactionUIData;

    private MonoBehaviour _storedItem;

    private string ItemName => toolType == ToolType.OxygenTank ? "Oxygen Tank" : "Blowtorch";
    private bool HasStoredItem => _storedItem != null && storagePoint != null
        && _storedItem.transform.parent == storagePoint;

    private void Start()
    {
        if (storagePoint == null || startingItem == null || startingItem is not IPickable item || !Accepts(item))
        {
            return;
        }

        Transform itemTransform = startingItem.transform;
        if (itemTransform.parent != null && itemTransform.parent != storagePoint)
        {
            return;
        }

        BoxCollider interactionCollider = GetComponent<BoxCollider>();
        var interactionBounds = new Bounds(interactionCollider.center, interactionCollider.size);
        if (interactionBounds.Contains(transform.InverseTransformPoint(itemTransform.position)))
        {
            Store(item);
        }
    }

    public void Interact(PlayerCharacter player)
    {
        InventorySystem inventory = player.InventorySystem;
        if (HasStoredItem)
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
        if (HasStoredItem)
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
