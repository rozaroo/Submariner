using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class OxygenTank : MonoBehaviour, IInteractable, IPickable
{
    [Header("Position Settings")]
    [SerializeField] private Vector3 holdOffset = new Vector3(-0.3f, -0.3f, 0.6f);
    
    [Header("Charge Properties")]
    [SerializeField] private float maxCharge = 100f;
    [SerializeField] private float currentCharge;
    [SerializeField] private Transform chargeBar;
    
    private Collider[] _colliders;
    private Rigidbody _rb;

    public GameObject GameObject => gameObject;
    public Vector3 HoldPositionOffset => holdOffset;
    
    private float CurrentCharge => currentCharge;
    public bool isEmpty => currentCharge <= 0f;
    public bool isFull => CurrentCharge >= maxCharge;

    private Coroutine _tankRecharge;
    private Vector3 _barOriginalScale;
    private Vector3 _barOriginalPosition;
    
    private void Awake()
    {
        _colliders = GetComponentsInChildren<Collider>();
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
        player.InventorySystem.TryPickUp(this);
    }

    public void OnPickUp()
    {
        SetCollidersEnabled(false);
        _rb.detectCollisions = false;
        if (!_rb.isKinematic)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
        _rb.isKinematic  = true;
    }

    public void OnDrop()
    {
        _rb.isKinematic   = false;
        _rb.detectCollisions = true;
        SetCollidersEnabled(true);
    }
    
    public void Dock()
    {
        SetCollidersEnabled(false);
        _rb.detectCollisions = false;
        if (!_rb.isKinematic)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
        _rb.isKinematic = true;
        transform.SetParent(null);
    }

    private void SetCollidersEnabled(bool isEnabled)
    {
        if (_colliders == null) return;
        foreach (var col in _colliders)
        {
            if (col != null) col.enabled = isEnabled;
        }
    }
    
    public float Drain(float amount)
    {
        float drained = Mathf.Min(amount, currentCharge);
        currentCharge = Mathf.Max(0f, currentCharge - drained);
        RefreshBar();
        return drained;
    }

    public void Refill(float amount)
    {
        currentCharge = Mathf.MoveTowards(currentCharge, maxCharge, amount);
        RefreshBar();
    }
    
    private float _lastBarRatio = -1f;

    public void StartRefill(float rate)
    {
        if (_tankRecharge != null) StopCoroutine(_tankRecharge);
        _tankRecharge = StartCoroutine(RefillOxygen(rate));
    }
    
    public void StopRefill()
    {
        if (_tankRecharge == null) return;
        StopCoroutine(_tankRecharge);
        _tankRecharge = null;
    }

    private IEnumerator RefillOxygen(float ratePerSecond)
    {
        while (!isFull)
        {
            currentCharge = Mathf.MoveTowards(currentCharge, maxCharge, ratePerSecond * Time.deltaTime);
            RefreshBar();
            yield return null;
        }
        currentCharge = maxCharge;
        RefreshBar();
    }

    private void RefreshBar()
    {
        if (chargeBar == null) return;

        float ratio = Mathf.Clamp01(currentCharge / maxCharge);
        if (Mathf.Abs(ratio - _lastBarRatio) < 0.001f && _lastBarRatio >= 0) return;
        _lastBarRatio = ratio;

        chargeBar.localScale = new Vector3(
            _barOriginalScale.x,
            _barOriginalScale.y * ratio,
            _barOriginalScale.z
        );
        
        chargeBar.localPosition = new Vector3(
            _barOriginalPosition.x,
            _barOriginalPosition.y - (_barOriginalScale.y * (1f - ratio)) / 2f,
            _barOriginalPosition.z
        );
    }
}
