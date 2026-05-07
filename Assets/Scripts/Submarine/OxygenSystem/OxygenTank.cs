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
    
    private Collider _collider;
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
        player.InventorySystem.TryPickUp(this);
    }

    public void OnPickUp()
    {
        _rb.isKinematic  = true;
        _collider.enabled = false;
    }

    public void OnDrop()
    {
        _rb.isKinematic   = false;
        _collider.enabled = true;
    }
    
    public void Dock()
    {
        transform.SetParent(null);
        _rb.isKinematic   = true;
        _collider.enabled = false;
    }
    
    public float Drain(float amount)
    {
        float drained = Mathf.Min(amount, currentCharge);
        currentCharge = Mathf.Max(0f, currentCharge - drained);
        RefreshBar();
        return drained;
    }
    
    public void StartRefill(float amount)
    {
        if (_tankRecharge != null) StopCoroutine(_tankRecharge);
        
        float remainingRatio = 1f - (currentCharge / maxCharge);
        float actualDuration = amount * remainingRatio;
        
        _tankRecharge = StartCoroutine(RefillOxygen(actualDuration));
    }
    
    public void StopRefill()
    {
        if (_tankRecharge != null)
        {
            StopCoroutine(_tankRecharge);
            _tankRecharge = null;
        }
    }

    private IEnumerator RefillOxygen(float duration)
    {
        float startCharge = currentCharge;
        float elapsedTime = 0f;

        while (elapsedTime < duration && !isFull)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            currentCharge = Mathf.Lerp(startCharge, maxCharge, t);
            Log.Info(currentCharge + " / " + maxCharge);
            RefreshBar();
            yield return null;
        }
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
        
        chargeBar.localPosition = new Vector3(
            _barOriginalPosition.x,
            _barOriginalPosition.y - (_barOriginalScale.y * (1f - ratio)) / 2f,
            _barOriginalPosition.z
        );
    }
}
