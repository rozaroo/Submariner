using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Blowtorch : MonoBehaviour, IInteractable
{
    [Header("Hold Position")]
    [SerializeField] private Vector3 holdOffset = new Vector3(0.3f, -0.3f, 0.6f);

    [Header("Repair")]
    [SerializeField] private float repairRange = 2.5f;
    [SerializeField] private LayerMask crackLayer;

    private PlayerCharacter _player;
    private Camera _camera;
    private Collider _collider;
    private Rigidbody _rb;
    private bool _isHeld;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _rb = GetComponent<Rigidbody>();
    }

    public void Interact(PlayerCharacter player)
    {
        if (!_isHeld) PickUp(player);
    }

    private void PickUp(PlayerCharacter player)
    {
        _player = player;
        _camera = player.CamController.MainCamera;
        _isHeld = true;
        
        _rb.isKinematic = true;
        _collider.enabled = false;

        transform.SetParent(_camera.transform);
        transform.localPosition = holdOffset;
        transform.localRotation = Quaternion.identity;
    }

    private void DropObject()
    {
        _isHeld = false;

        transform.SetParent(null);
        
        _rb.isKinematic = false;
        _collider.enabled = true;

        _player = null;
        _camera = null;
    }

    private void Update()
    {
        if (!_isHeld || _player == null) return;
        
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            DropObject();
            return;
        }
        
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, repairRange, crackLayer))
            {
                if (hit.collider.TryGetComponent(out HullDamage crack))
                    crack.Repair(Time.deltaTime);
            }
        }
    }
}
