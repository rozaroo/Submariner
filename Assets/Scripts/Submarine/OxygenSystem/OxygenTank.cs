using UnityEngine;
using UnityEngine.InputSystem;

// Recoger con E (debe estar en la layer interactable), soltar con Q.
// Llevar a una OxygenTerminal y presionar E para colocarlo.
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class OxygenTank : MonoBehaviour, IInteractable
{
    [Header("Carga")]
    [SerializeField] private float maxCharge = 100f;
    [SerializeField] private float currentCharge;
    [SerializeField] private Transform chargeBar;

    [Header("Hold Position")]
    [SerializeField] private Vector3 holdOffset = new Vector3(-0.3f, -0.3f, 0.6f);

    public float CurrentCharge => currentCharge;
    public bool IsEmpty => currentCharge <= 0f;

    // Referencia al tanque que el jugador está sosteniendo actualmente
    public static OxygenTank CurrentHeld { get; private set; }

    private PlayerCharacter _player;
    private Camera _camera;
    private Collider _collider;
    private Rigidbody _rb;
    private bool _isHeld;

    private Vector3 _barOriginalScale;
    private Vector3 _barOriginalPosition;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _rb       = GetComponent<Rigidbody>();
        currentCharge = maxCharge;

        if (chargeBar != null)
        {
            _barOriginalScale    = chargeBar.localScale;
            _barOriginalPosition = chargeBar.localPosition;
        }

        RefreshBar();
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
        CurrentHeld = this;

        _rb.isKinematic  = true;
        _collider.enabled = false;

        transform.SetParent(_camera.transform);
        transform.localPosition = holdOffset;
        transform.localRotation = Quaternion.identity;
    }

    public void Drop()
    {
        _isHeld = false;
        if (CurrentHeld == this) CurrentHeld = null;

        transform.SetParent(null);
        _rb.isKinematic   = false;
        _collider.enabled = true;

        _player = null;
        _camera = null;
    }

    // Llamado por OxygenTerminal: saca el tanque de la mano y lo fija en el dock
    public void Dock()
    {
        _isHeld = false;
        if (CurrentHeld == this) CurrentHeld = null;

        transform.SetParent(null);
        _rb.isKinematic   = true;
        _collider.enabled = false;

        _player = null;
        _camera = null;
    }

    public bool IsFull => CurrentCharge >= maxCharge;

    // Llamado por OxygenTerminal para drenar la carga del tanque
    public float Drain(float amount)
    {
        float drained = Mathf.Min(amount, currentCharge);
        currentCharge = Mathf.Max(0f, currentCharge - drained);
        RefreshBar();
        return drained;
    }

    // Llamado por TankRechargeTerminal para recargar el tanque
    public void Refill(float amount)
    {
        currentCharge = Mathf.Min(maxCharge, currentCharge + amount);
        RefreshBar();
    }

    private void RefreshBar()
    {
        if (chargeBar == null) return;

        float ratio = Mathf.Clamp01(currentCharge / maxCharge);

        chargeBar.localScale = new Vector3(
            _barOriginalScale.x,
            _barOriginalScale.y * ratio,
            _barOriginalScale.z
        );

        // Se mueve hacia abajo para que el tope quede fijo
        chargeBar.localPosition = new Vector3(
            _barOriginalPosition.x,
            _barOriginalPosition.y - (_barOriginalScale.y * (1f - ratio)) / 2f,
            _barOriginalPosition.z
        );
    }

    private void Update()
    {
        if (!_isHeld || _player == null) return;

        if (Keyboard.current.qKey.wasPressedThisFrame)
            Drop();
    }
}
