using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class LeverStation : MonoBehaviour, IStationControl
{
    [Header("Lever Settings")]
    [SerializeField] private float _pullSpeed = 0.5f;
    [SerializeField] private float _maxAngle = 90f;
    public bool IsUnlocked { get; set; }
    public bool IsPressed { get; set; }
    public Action OnActivation { get; set; }
    private float _currentAngle = 0f;

    public void Lock() => IsUnlocked = false;
    public void Unlock() => IsUnlocked = true;

    public void OnActionDown() { }

    public void OnActionDrag(float deltaY)
    {
        if (!IsUnlocked) return;
        if (deltaY < 0)
        {
            _currentAngle += Mathf.Abs(deltaY) * _pullSpeed;
            _currentAngle = Mathf.Clamp(_currentAngle, 0f, _maxAngle);
            
            transform.localRotation = Quaternion.Euler(0f, 0f, _currentAngle);
            
            if (_currentAngle >= _maxAngle)
            {
                IsUnlocked = false;
                OnActivation?.Invoke();
            }
        }
    }

    public void OnActionUp() { }

    public void RestartButton()
    {
        Lock();
        
    }
}