using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class LeverPullStation : MonoBehaviour, ILeverControls
{
    [Header("Lever Settings")]
    [SerializeField] private float pullSpeed = 0.5f;
    [SerializeField] private float maxAngleActivation = 90f;
    [SerializeField, Tooltip("Use -1 to mirror the lever's visual rotation without changing its input direction.")]
    private float rotationDirection = 1f;
    
    private float _initialAngle;
    private float _currentAngle = 0f;
    
    private Quaternion _initialRotation;
    
    public bool isActive { get; set; }
    public bool isLocked { get; set; }
    public Action onActivation { get; set; }
    public Action onDeactivation { get; set; }

    private void Awake()
    {
        _initialRotation = transform.localRotation;
        _currentAngle = 0f;
        Lock();
    }
    
    public void Lock() => isLocked = true;
    public void Unlock() => isLocked = false;
    
    public void SetActive(bool active)
    {
        Log.Info($"Lever {(active ? "Activated" : "Deactivated")} - {gameObject.name}");
        if (active)
        {
            isActive = true;
            onActivation?.Invoke();
            SFXManager.PostEvent("Start_LeverPullFinished", gameObject);
            SetLeverRotation(maxAngleActivation);
        }
        else
        {
            isActive = false;
            onDeactivation?.Invoke();
            SetLeverRotation(0f);
        }
    }

    public void OnActionDrag(float delta)
    {
        if (isLocked) return;
        _currentAngle = Mathf.Clamp(_currentAngle - delta * pullSpeed, 0f, maxAngleActivation);
        SetLeverRotation(_currentAngle);

        if (_currentAngle >= maxAngleActivation && !isActive)
        {
            Log.Info("Lever reached activation angle.");
            SetActive(true);
        }
        else if (_currentAngle <= 0f && isActive)
        {
            Log.Info("Lever returned to initial position.");
            SetActive(false);
        }
    }

    public void Restart()
    {
        Lock();
        isActive = false;
        SetLeverRotation(0f);
    }

    private void SetLeverRotation(float angle)
    {
        float direction = Mathf.Approximately(rotationDirection, 0f) ? 1f : Mathf.Sign(rotationDirection);
        transform.localRotation = _initialRotation * Quaternion.Euler(0f, 0f, angle * direction);
        _currentAngle = angle;
    }
}
