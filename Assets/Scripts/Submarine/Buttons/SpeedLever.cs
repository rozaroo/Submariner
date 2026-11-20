using System;
using UnityEngine;

public class SpeedLever : MonoBehaviour, ILeverControls, ILeverDragRelease
{
    [Header("References")]
    [SerializeField] private SubmarineMovement submarineMovement;

    [Header("Movement")]
    [SerializeField] private Vector3 movementAxis = Vector3.right;
    [SerializeField] private float dragSensitivity = 0.01f;

    [Header("Position Settings")]
    [SerializeField] private float inactivePosition = 0f;
    [SerializeField] private float normalPosition = 0.03f;
    [SerializeField] private float reversePosition = -0.03f;
    [SerializeField] private float urgencyPosition = 0.06f;

    private Vector3 _initialLocalPosition;
    private Vector3 _normalizedAxis;
    private float _currentPosition;

    private SubmarineSpeedMode _currentMode = SubmarineSpeedMode.Inactive;

    public bool isActive { get; set; }
    public bool isLocked { get; set; }

    public Action onActivation { get; set; }
    public Action onDeactivation { get; set; }

    public SubmarineSpeedMode CurrentMode => _currentMode;

    private void Awake()
    {
        _initialLocalPosition = transform.localPosition;
        _normalizedAxis = movementAxis.normalized;
        if (_normalizedAxis.sqrMagnitude < 0.001f) _normalizedAxis = Vector3.right;
        _currentPosition = inactivePosition;
        UpdateLeverPosition();
    }

    private void Start()
    {
        if (submarineMovement == null)
        {
            Debug.LogError("[SpeedLever] SubmarineMovement no está asignado.");
            return;
        }
        SetSpeedMode(SubmarineSpeedMode.Inactive);
    }

    private void OnEnable()
    {
        if (submarineMovement != null) submarineMovement.OnSpeedModeChanged += HandleSpeedModeChanged;
    }
    private void OnDisable()
    {
        if (submarineMovement != null) submarineMovement.OnSpeedModeChanged -= HandleSpeedModeChanged;
    }
    public void OnActionDrag(float delta)
    {
        if (isLocked || submarineMovement == null) return;
        if (!submarineMovement.IsEngineRunning()) return;
        float minPosition = Mathf.Min(inactivePosition,normalPosition,reversePosition,urgencyPosition);
        float maxPosition = Mathf.Max(inactivePosition,normalPosition,reversePosition,urgencyPosition);
        _currentPosition += delta * dragSensitivity;
        _currentPosition = Mathf.Clamp(_currentPosition,minPosition,maxPosition);
        UpdateLeverPosition();
    }

    public void OnActionRelease()
    {
        if (isLocked || submarineMovement == null) return;
        if (!submarineMovement.IsEngineRunning())
        {
            _currentPosition = inactivePosition;
            UpdateLeverPosition();
            return;
        }
        float closestDistance = Mathf.Abs(_currentPosition - inactivePosition);
        SubmarineSpeedMode closestMode = SubmarineSpeedMode.Inactive;
        float closestPosition = inactivePosition;
        CheckClosestPosition(normalPosition,SubmarineSpeedMode.Normal,ref closestDistance,ref closestMode,ref closestPosition);

        CheckClosestPosition(
            reversePosition,
            SubmarineSpeedMode.Reverse,
            ref closestDistance,
            ref closestMode,
            ref closestPosition
        );

        CheckClosestPosition(
            urgencyPosition,
            SubmarineSpeedMode.Urgency,
            ref closestDistance,
            ref closestMode,
            ref closestPosition
        );

        _currentPosition = closestPosition;
        UpdateLeverPosition();

        SetSpeedMode(closestMode);
    }

    private void CheckClosestPosition(
        float position,
        SubmarineSpeedMode mode,
        ref float closestDistance,
        ref SubmarineSpeedMode closestMode,
        ref float closestPosition)
    {
        float distance = Mathf.Abs(_currentPosition - position);

        if (distance < closestDistance)
        {
            closestDistance = distance;
            closestMode = mode;
            closestPosition = position;
        }
    }

    private void UpdateLeverPosition()
    {
        transform.localPosition =
            _initialLocalPosition + _normalizedAxis * _currentPosition;
    }

    public void SetSpeedMode(SubmarineSpeedMode mode)
    {
        if (submarineMovement == null || isLocked) return;
        if (!submarineMovement.IsEngineRunning()) return;
        submarineMovement.SetSpeedMode(mode);
        Debug.Log($"[SpeedLever] Speed mode: {submarineMovement.CurrentSpeedMode}");
    }

    public void SetActive(bool active)
    {
        SetSpeedMode(active ? SubmarineSpeedMode.Normal : SubmarineSpeedMode.Inactive);
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
        bool wasActive = isActive;
        isActive = false;
        _currentMode = SubmarineSpeedMode.Inactive;
        _currentPosition = inactivePosition;
        UpdateLeverPosition();
        Lock();
        if (submarineMovement != null) submarineMovement.SetSpeedMode(SubmarineSpeedMode.Inactive);
        if (wasActive) onDeactivation?.Invoke();
    }
    private void HandleSpeedModeChanged(SubmarineSpeedMode mode)
    {
        bool wasActive = isActive;

        _currentMode = mode;
        isActive = mode != SubmarineSpeedMode.Inactive;

        _currentPosition = mode switch
        {
            SubmarineSpeedMode.Normal => normalPosition,
            SubmarineSpeedMode.Reverse => reversePosition,
            SubmarineSpeedMode.Urgency => urgencyPosition,
            _ => inactivePosition
        };

        UpdateLeverPosition();

        if (!wasActive && isActive) onActivation?.Invoke();
        else if (wasActive && !isActive) onDeactivation?.Invoke();
    }
}
