using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider))]
public class LeverPullStation : MonoBehaviour, ILeverControls
{
    [Header("Lever Settings")]
    [SerializeField] private float pullSpeed = 0.5f;
    [SerializeField] private float maxAngleActivation = 90f;
    
    private float _initialAngle;
    private float _currentAngle = 0f;
    
    public bool isActive { get; set; }
    public bool isLocked { get; set; }
    public bool IsUnlocked => !isLocked;
    [SerializeField] private UnityEvent onActivationEvent;
    [SerializeField] private UnityEvent onDeactivationEvent;
    public UnityEvent onActivation => onActivationEvent;
    public UnityEvent onDeactivation => onDeactivationEvent;

    private void Awake()
    {
        _initialAngle = transform.localRotation.eulerAngles.z;
        _currentAngle = _initialAngle;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log("[DEBUG] F1 -> Activating Lever");
            SetActive(true);
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            Debug.Log("[DEBUG] F2 -> Deactivating Lever");
            SetActive(false);
        }
    }
    public void Lock() => isLocked = true;
    public void Unlock() => isLocked = false;

    public void SetActive(bool active)
    {
        Log.Info($"[LEVER] SetActive({active})");
        //Log.Info($"Lever {(active ? "Activated" : "Deactivated")} - {gameObject.name}");
        if (active)
        {
            isActive = true;
            onActivationEvent?.Invoke();
            SFXManager.PostEvent("Start_LeverPullFinished", gameObject);
            SetLeverRotation(maxAngleActivation);
        }
        else
        {
            isActive = false;
            onDeactivationEvent?.Invoke();
            SetLeverRotation(_initialAngle);
        }
    }

    public void OnActionDrag(float delta)
    {
        if (isLocked)
        {
            Log.Info("Lever is locked.");
            return;
        }
        if (Mathf.Abs(delta) < 0.001f) return;
        _currentAngle -= delta * pullSpeed;
        _currentAngle = Mathf.Clamp(_currentAngle, _initialAngle, maxAngleActivation);
        transform.localRotation = Quaternion.Euler(0f, 0f, _currentAngle);

        if (_currentAngle >= maxAngleActivation && !isActive)
        {
            Log.Info("Lever reached activation angle.");
            SetActive(true);
        }
        else if (_currentAngle <= _initialAngle && isActive)
        {
            Log.Info("Lever returned to initial position.");
            SetActive(false);
        }
    }

    public void Restart()
    {
        Lock();
        isActive = false;
        SetLeverRotation(_initialAngle);
    }

    private void SetLeverRotation(float angle)
    {
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        _currentAngle = angle;
    }
}