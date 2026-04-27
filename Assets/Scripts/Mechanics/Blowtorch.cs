using UnityEngine;
using UnityEngine.InputSystem;

// Levantar con E (el objeto debe estar en la layer interactable del PlayerCharacter).
// Sostenido: mantener click izquierdo apuntando a una Crack para repararla. Q para soltar.
[RequireComponent(typeof(Collider))]
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
    private bool _isHeld;

    private void Awake() => _collider = GetComponent<Collider>();

    public void Interact(PlayerCharacter player)
    {
        if (!_isHeld) PickUp(player);
    }

    private void PickUp(PlayerCharacter player)
    {
        _player  = player;
        _camera  = player.CamController.MainCamera;
        _isHeld  = true;
        _collider.enabled = false;

        // Se parentea a la cámara para simular que el jugador lo sostiene
        transform.SetParent(_camera.transform);
        transform.localPosition = holdOffset;
        transform.localRotation = Quaternion.identity;
    }

    private void Drop()
    {
        _isHeld = false;
        _collider.enabled = true;
        transform.SetParent(null);
        _player = null;
        _camera = null;
    }

    private void Update()
    {
        if (!_isHeld || _player == null) return;

        // Q → soltar el soplete
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            Drop();
            return;
        }

        // Click sostenido → reparar la grieta que está en frente
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, repairRange, crackLayer))
            {
                if (hit.collider.TryGetComponent(out Crack crack))
                    crack.Repair(Time.deltaTime);
            }
        }
    }
}
