using System;
using UnityEngine;

public class AngleLever : MonoBehaviour, ILeverControls, ILeverDragRelease
{
    [Header("References")]
    [SerializeField] private SubmarineMovement submarineMovement;

    [Header("Angle Settings")]
    [SerializeField] private float minAngle = -45f;
    [SerializeField] private float maxAngle = 45f;

    [Header("Movement")]
    [SerializeField] private Vector3 movementAxis = Vector3.right;
    [SerializeField] private float dragSensitivity = 0.01f;

    [Header("Position Settings")]
    [SerializeField] private float minPosition = -0.03f;
    [SerializeField] private float maxPosition = 0.03f;

    private Vector3 _initialLocalPosition;
    private Vector3 _normalizedAxis;
    private float _currentPosition;
    private float _currentAngle;

    public float CurrentAngle => _currentAngle;

    public bool isActive { get; set; }
    public bool isLocked { get; set; }

    public Action onActivation { get; set; }
    public Action onDeactivation { get; set; }

    private void Awake()
    {
        _initialLocalPosition = transform.localPosition;
        _normalizedAxis = movementAxis.normalized;
        if (_normalizedAxis.sqrMagnitude < 0.001f) _normalizedAxis = Vector3.right;
        UpdateLeverPosition();
    }

    private void Start()
    {
        if (submarineMovement == null)
        {
            Debug.LogError("[AngleLever] SubmarineMovement no está asignado.");
            return;
        }

        SetAngle(submarineMovement.CurrentAngle);
    }

    public void OnActionDrag(float delta)
    {
        if (isLocked || submarineMovement == null) return;
        if (!submarineMovement.IsEngineRunning()) return;

        _currentPosition += delta * dragSensitivity;

        _currentPosition = Mathf.Clamp(
            _currentPosition,
            minPosition,
            maxPosition
        );

        float normalizedPosition = Mathf.InverseLerp(
            minPosition,
            maxPosition,
            _currentPosition
        );

        float targetAngle = Mathf.Lerp(
            minAngle,
            maxAngle,
            normalizedPosition
        );

        SetAngle(targetAngle);
    }

    public void OnActionRelease()
    {
        // El ángulo se conserva al soltar la palanca.
    }

    public void SetAngle(float angle)
    {
        if (submarineMovement == null) return;

        submarineMovement.SetAngle(
            Mathf.Clamp(angle, minAngle, maxAngle)
        );

        _currentAngle = submarineMovement.CurrentAngle;

        float normalizedAngle = Mathf.InverseLerp(
            minAngle,
            maxAngle,
            _currentAngle
        );

        _currentPosition = Mathf.Lerp(
            minPosition,
            maxPosition,
            normalizedAngle
        );

        UpdateLeverPosition();

        bool wasActive = isActive;
        isActive = !Mathf.Approximately(_currentAngle, 0f);

        if (!wasActive && isActive)
            onActivation?.Invoke();
        else if (wasActive && !isActive)
            onDeactivation?.Invoke();
    }

    private void UpdateLeverPosition()
    {
        transform.localPosition =
            _initialLocalPosition + _normalizedAxis * _currentPosition;
    }

    public void SetActive(bool active)
    {
        if (active) return;
        SetAngle(0f);
    }

    public void Lock()
    {
        isLocked = true;
    }

    public void Unlock()
    {
        isLocked = false;
    }

    public void Restart()
    {
        _currentAngle = 0f;
        _currentPosition = Mathf.InverseLerp(minAngle, maxAngle, 0f);
        _currentPosition = Mathf.Lerp(minPosition, maxPosition, _currentPosition);
        UpdateLeverPosition();
        isActive = false;
        Lock();
        if (submarineMovement != null) submarineMovement.SetAngle(0f);
        onDeactivation?.Invoke();
    }
}
