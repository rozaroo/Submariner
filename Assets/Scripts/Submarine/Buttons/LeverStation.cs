using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class LeverStation : MonoBehaviour, ILeverControls
{
    [Header("Lever Settings")]
    [SerializeField] private float pullSpeed = 0.5f;
    [SerializeField] private float maxAngleActivation = 90f;
    
    private float _initialAngle;
    private float _currentAngle = 0f;
    
    public bool isActive { get; set; }
    public bool isLocked { get; set; }
    public Action onActivation { get; set; }
    public Action onDeactivation { get; set; }

    private void Awake()
    {
        _initialAngle = transform.localRotation.eulerAngles.y;
    }
    
    public void Lock() => isLocked = true;
    public void Unlock() => isLocked = false;
    
    public void SetActive(bool active)
    {
        if (active)
        {
            isActive = true;
            onActivation?.Invoke();
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
        transform.localRotation = Quaternion.Euler(0f, 0f, _currentAngle);

        if (_currentAngle >= maxAngleActivation && !isActive)
        {
            SetActive(true);
        }
        else if (_currentAngle <= _initialAngle && isActive)
        {
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