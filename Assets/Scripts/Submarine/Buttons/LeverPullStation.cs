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
    
    public bool isActive { get; set; }
    public bool isLocked { get; set; }
    public Action onActivation { get; set; }
    public Action onDeactivation { get; set; }

    private void Awake()
    {
        _initialAngle = transform.localRotation.eulerAngles.z;
        _currentAngle = _initialAngle;
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
            SetLeverRotation(_initialAngle);
        }
    }

    public void OnActionDrag(float delta)
    {
        if (isLocked) return;
        _currentAngle -= delta * pullSpeed;
        _currentAngle = Mathf.Clamp(_currentAngle, _initialAngle, maxAngleActivation);
        // Keep the drag preview on the same side as the configured pull
        // direction; SetLeverRotation applies the visual mirror when needed.
        SetLeverRotation(_currentAngle);

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
        float direction = Mathf.Approximately(rotationDirection, 0f) ? 1f : Mathf.Sign(rotationDirection);
        float visualAngle = _initialAngle + ((angle - _initialAngle) * direction);
        transform.localRotation = Quaternion.Euler(0f, 0f, visualAngle);
        _currentAngle = angle;
    }
}
