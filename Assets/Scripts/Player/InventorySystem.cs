using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    [Header("System Properties")]
    [SerializeField] private bool isHoldingObject;
    [SerializeField] private Camera _heldObjectPlacement;
    
    private IPickable _heldItem;
    private bool isHoldingItem => _heldItem != null;

    public bool TryPickUp(IPickable item)
    {
        if (isHoldingItem) return false;

        _heldItem = item;
        _heldItem.OnPickUp();
        _heldItem.GameObject.transform.SetParent(_heldObjectPlacement.transform);
        _heldItem.GameObject.transform.localPosition = item.HoldPositionOffset;
        _heldItem.GameObject.transform.localRotation = Quaternion.identity;
        return true;
    }
    
    public void DropItem()
    {
        if (!isHoldingItem) return;
        _heldItem.GameObject.transform.SetParent(null);
        _heldItem.OnDrop();
        _heldItem = null;
    }
    
    public bool UseItem()
    {
        if (!isHoldingItem) return false;
        if (_heldItem is IUsable usableItem)
        {
            usableItem.UseItem();
            return true;
        }
        return false;
    }
    
    public void UseItemHold()
    {
        if (!isHoldingItem) return;
        if (_heldItem is IUsable usableItem)
            usableItem.UseItemHold();
    }

    public void UseItemReleased()
    {
        if (!isHoldingItem) return;
        if (_heldItem is IUsable usableItem)
            usableItem.UseItemReleased();
    }

    public bool TryGetHeldItem<T>(out T item) where T : class
    {
        item = _heldItem as T;
        if (item != null)
        {
            DropItem();
            return item != null;
        }
        return false;
    }
}
