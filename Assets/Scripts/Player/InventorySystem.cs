using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    private const float ToolClearance = 0.015f;

    [Header("System Properties")]
    [SerializeField] private bool isHoldingObject;
    [SerializeField] private Camera _heldObjectPlacement;
    
    private IPickable _heldItem;
    private Vector3 _heldItemWorldScale;
    private BoxCollider _heldToolCollider;
    private CharacterController _characterController;
    private int _toolObstructionLayers;
    private bool isHoldingItem => _heldItem != null;
    public IPickable HeldItem => _heldItem;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _toolObstructionLayers = Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Player", "Water", "WaterInterior");
    }

    public bool TryPickUp(IPickable item)
    {
        if (isHoldingItem) return false;

        _heldItem = item;
        Transform itemTransform = _heldItem.GameObject.transform;
        _heldItemWorldScale = itemTransform.lossyScale;
        
        itemTransform.SetParent(_heldObjectPlacement.transform, false);
        itemTransform.localPosition = item.HoldPositionOffset;
        itemTransform.localRotation = Quaternion.Euler(item.HoldRotationOffset);
        SetWorldScale(itemTransform, _heldItemWorldScale);
        _heldItem.OnPickUp();

        if (item is Blowtorch or OxygenTank)
            item.GameObject.TryGetComponent(out _heldToolCollider);

        return true;
    }
    
    public void DropItem() => TryDropHeldItem();

    private bool TryDropHeldItem()
    {
        if (!isHoldingItem) return false;

        if (_heldToolCollider != null && IsToolDropObstructed() && !TryPlaceToolAtFeet()) return false;

        ClearHeldItem(true);
        return true;
    }

    private Vector3 GetHeldToolHalfExtents()
    {
        Vector3 size = Vector3.Scale(_heldToolCollider.size, _heldItemWorldScale);
        return new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z)) * 0.5f
            + Vector3.one * ToolClearance;
    }

    private bool IsToolDropObstructed()
    {
        Transform tool = _heldToolCollider.transform;
        Vector3 origin = _heldObjectPlacement.transform.position;
        Vector3 center = tool.TransformPoint(_heldToolCollider.center);
        Vector3 halfExtents = GetHeldToolHalfExtents();
        const QueryTriggerInteraction ignoreTriggers = QueryTriggerInteraction.Ignore;

        if (Physics.CheckBox(center, halfExtents, tool.rotation, _toolObstructionLayers, ignoreTriggers)
            || Physics.Linecast(origin, center, _toolObstructionLayers, ignoreTriggers))
        {
            return true;
        }

        // A cast from the camera can start overlapped when the player is close to a surface behind the camera.
        Vector3 direction = center - origin;
        return !Physics.CheckBox(origin, halfExtents, tool.rotation, _toolObstructionLayers, ignoreTriggers)
            && Physics.BoxCast(origin, halfExtents, direction.normalized, tool.rotation, direction.magnitude,
                _toolObstructionLayers, ignoreTriggers);
    }

    private bool TryPlaceToolAtFeet()
    {
        if (_characterController == null) return false;

        Bounds playerBounds = _characterController.bounds;
        Vector3 center = playerBounds.center;
        Quaternion rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        Vector3 halfExtents = GetHeldToolHalfExtents();
        float distance = Mathf.Max(0f, center.y - playerBounds.min.y - halfExtents.y - ToolClearance);
        const QueryTriggerInteraction ignoreTriggers = QueryTriggerInteraction.Ignore;

        if (Physics.CheckBox(center, halfExtents, rotation, _toolObstructionLayers, ignoreTriggers)) return false;
        if (distance > 0f && Physics.BoxCast(center, halfExtents, Vector3.down, out RaycastHit hit,
                rotation, distance, _toolObstructionLayers, ignoreTriggers))
        {
            distance = Mathf.Max(0f, hit.distance - ToolClearance);
        }

        center += Vector3.down * distance;
        Vector3 centerOffset = rotation * Vector3.Scale(_heldToolCollider.center, _heldItemWorldScale);
        _heldToolCollider.transform.SetPositionAndRotation(center - centerOffset, rotation);
        return true;
    }

    private void ClearHeldItem(bool triggerOnDrop)
    {
        if (!isHoldingItem) return;

        _heldToolCollider = null;
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
        if (item != null && TryDropHeldItem()) return true;
        item = null;
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
        if (Mathf.Approximately(parentScale, 0f)) return targetScale;
        return targetScale / parentScale;
    }
    public bool IsHolding<T>() where T : class
    {
        return _heldItem is T;
    }
}
