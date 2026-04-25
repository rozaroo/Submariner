using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ButtonStation : MonoBehaviour, IStationControl
{
    [Header("Button Settings")]
    public Action OnActivation { get; set; }
    
    private bool _isPressed = false;
    
    public void OnPointerDown()
    {
        if (_isPressed) return;
        _isPressed = true;
        // TODO: Sound/Animation
        OnActivation?.Invoke();
    }

    public void OnPointerDrag(float deltaY) { }
    public void OnPointerUp() { }

    public void RestartButton()
    {
        _isPressed = false;
    }
}