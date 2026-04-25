using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LeverStation : MonoBehaviour, IStationControl
{
    [Header("Lever Settings")]
    [SerializeField] private float _pullSpeed = 0.5f;
    [SerializeField] private float _maxAngle = 90f;
    
    public Action OnActivation { get; set; }
    
    private float _currentAngle = 0f;
    private bool _isUnlocked = false;

    public void UnlockLever() => _isUnlocked = true;
    public void OnPointerDown() { }

    public void OnPointerDrag(float deltaY)
    {
        if (!_isUnlocked) return;
        
        if (deltaY < 0)
        {
            _currentAngle += Mathf.Abs(deltaY) * _pullSpeed;
            _currentAngle = Mathf.Clamp(_currentAngle, 0f, _maxAngle);
            
            transform.localRotation = Quaternion.Euler(_currentAngle, 0f, 0f);
            
            if (_currentAngle >= _maxAngle)
            {
                OnActivation?.Invoke();
                _isUnlocked = false;
            }
        }
    }

    public void OnPointerUp() { }
    
    public void RestartButton() { }
}