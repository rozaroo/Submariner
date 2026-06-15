using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    [Header("System Properties")]
    [SerializeField] private bool isHoldingObject;
    [SerializeField] private Camera _heldObjectPlacement;
    
    private IPickable _heldItem;
    private Vector3 _heldItemWorldScale;
    private bool isHoldingItem => _heldItem != null;

    public bool TryPickUp(IPickable item)
    {
        if (isHoldingItem) return false;

        _heldItem = item;
        Transform itemTransform = _heldItem.GameObject.transform;
        _heldItemWorldScale = itemTransform.lossyScale;

        _heldItem.OnPickUp();
        itemTransform.SetParent(_heldObjectPlacement.transform, false);
        itemTransform.localPosition = item.HoldPositionOffset;
        itemTransform.localRotation = Quaternion.identity;
        SetWorldScale(itemTransform, _heldItemWorldScale);
        return true;
    }
    
    public void DropItem()
    {
        ClearHeldItem(true);
    }

    private void ClearHeldItem(bool triggerOnDrop)
    {
        if (!isHoldingItem) return;

        Transform itemTransform = _heldItem.GameObject.transform;
        itemTransform.SetParent(null, true);
        SetWorldScale(itemTransform, _heldItemWorldScale);
        
        if (triggerOnDrop) 
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

    public bool TryExtractHeldItem<T>(out T item) where T : class
    {
        item = _heldItem as T;
        if (item != null)
        {
            ClearHeldItem(false);
            return true;
        }
        return false;
    }

    private void SetWorldScale(Transform targetTransform, Vector3 worldScale)
    {
        if (targetTransform.parent == null)
        {
            targetTransform.localScale = worldScale;
            return;
        }

        Vector3 parentScale = targetTransform.parent.lossyScale;
        targetTransform.localScale = new Vector3(
            GetSafeScaleValue(worldScale.x, parentScale.x),
            GetSafeScaleValue(worldScale.y, parentScale.y),
            GetSafeScaleValue(worldScale.z, parentScale.z)
        );
    }

    private float GetSafeScaleValue(float targetScale, float parentScale)
    {
        if (Mathf.Approximately(parentScale, 0f))
        {
            return targetScale;
        }

        return targetScale / parentScale;
    }
}
