
using System.Collections.Generic;
using UnityEngine;

public enum SubmarineSpeedMode
{
    Inactive,
    Normal,
    Reverse,
    Urgency
}

[RequireComponent(typeof(Rigidbody))]
public class SubmarineMovement : MonoBehaviour
{
    [Header("Speed Settings")]
    [SerializeField] private float normalSpeed = 5f;
    [SerializeField] private float reverseSpeed = -5f;
    [SerializeField] private float urgencySpeed = 10f;

    [Header("Movement Settings")]
    [SerializeField] private float accelerationTime = 2f;
    [SerializeField] private float decelerationTime = 3f;
    [SerializeField] private float rotationSmoothTime = 0.5f;
    [SerializeField] private float distanceOffset = 0.5f;

    [Header("Collision Properties")]
    [SerializeField] private float bounceFactor = 0.4f;

    private Rigidbody _rb;
    private Vector3 _previousPosition;
    private List<Vector3> _currentWaypoints = new();

    private int _currentIndex;
    private bool _hasTarget;

    private Vector3 _velocity;

    private float _currentSpeed;
    private float _targetSpeed;
    private float _currentAngle;

    private float _speedMultiplier = 1f;

    private EngineState _currentEngineState = EngineState.Off;
    private SubmarineSpeedMode _speedMode = SubmarineSpeedMode.Inactive;

    private bool _isEmergencyBraking;
    public bool IsEmergencyBraking => _isEmergencyBraking;
    public float CurrentSpeed => _currentSpeed;
    public float CurrentAngle => _currentAngle;
    public SubmarineSpeedMode CurrentSpeedMode => _speedMode;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        GameEventChannel<OnSubmarineRouteChanged>.OnEventRaised += GetNewWaypointList;
        GameEventChannel<OnSubmarineCollision>.OnEventRaised += OnSubmarineCollision;
        GameEventChannel<OnEngineStateChanged>.OnEventRaised += HandleEngineStateChanged;
    }

    private void OnDisable()
    {
        GameEventChannel<OnSubmarineRouteChanged>.OnEventRaised -= GetNewWaypointList;
        GameEventChannel<OnSubmarineCollision>.OnEventRaised -= OnSubmarineCollision;
        GameEventChannel<OnEngineStateChanged>.OnEventRaised -= HandleEngineStateChanged;
    }

    private void FixedUpdate()
    {
        _previousPosition = _rb.position;
        UpdateMovement();
        UpdateRotation();
        CheckWaypointArrival();
    }

    #region Engine

    private void HandleEngineStateChanged(OnEngineStateChanged data)
    {
        _currentEngineState = data.State;
        _speedMultiplier = data.SpeedMultiplier;
        if (!IsEngineRunning())
        {
            _speedMode = SubmarineSpeedMode.Inactive;
            _targetSpeed = 0f;
            _isEmergencyBraking = false;
        }
    }

    private bool IsEngineRunning()
    {
        return _currentEngineState == EngineState.Operative || _currentEngineState == EngineState.Degraded;
    }

    #endregion

    #region NavigationControls

    public void SetSpeedMode(SubmarineSpeedMode mode)
    {
        if (!IsEngineRunning())
        {
            _speedMode = SubmarineSpeedMode.Inactive;
            _targetSpeed = 0f;
            return;
        }

        if (_isEmergencyBraking)
            return;

        _speedMode = mode;

        switch (mode)
        {
            case SubmarineSpeedMode.Inactive:
                _targetSpeed = 0f;
                break;

            case SubmarineSpeedMode.Normal:
                _targetSpeed = normalSpeed;
                break;

            case SubmarineSpeedMode.Reverse:
                _targetSpeed = reverseSpeed;
                break;

            case SubmarineSpeedMode.Urgency:
                _targetSpeed = urgencySpeed;
                break;
        }
    }

    public void SetAngle(float angle)
    {
        if (!IsEngineRunning()) return;
        _currentAngle = Mathf.Clamp(angle, -45f, 45f);
    }

    #endregion

    #region Movement

    private void UpdateMovement()
    {
        Vector3 desiredVelocity = Vector3.zero;
        Vector3 movementDirection = Vector3.zero;
        bool hasMovementDirection = false;

        // Calculamos el rumbo hacia el waypoint.
        if (_hasTarget && _currentWaypoints != null && _currentIndex < _currentWaypoints.Count)
        {
            Vector3 routeDirection = _currentWaypoints[_currentIndex] - _rb.position;
            routeDirection.y = 0f;
            if (routeDirection.sqrMagnitude > 0.001f)
            {
                routeDirection.Normalize();
                movementDirection = Quaternion.Euler(0f, _currentAngle, 0f) * routeDirection;
                desiredVelocity = movementDirection * (_targetSpeed * _speedMultiplier);
                hasMovementDirection = true;
            }
        }
        // El freno de emergencia siempre tiene como objetivo detenerse.
        if (_isEmergencyBraking) desiredVelocity = Vector3.zero;
        float currentVelocityMagnitude = _velocity.magnitude;
        float desiredVelocityMagnitude = desiredVelocity.magnitude;
        float movementRate;
        // Determinamos si estamos acelerando o desacelerando.
        if (desiredVelocityMagnitude < currentVelocityMagnitude ||Vector3.Dot(_velocity, desiredVelocity) < 0f ||_isEmergencyBraking)
        {
            movementRate = normalSpeed / Mathf.Max(decelerationTime, 0.01f);
            if (_isEmergencyBraking) movementRate *= 2f;
        }
        else movementRate = urgencySpeed / Mathf.Max(accelerationTime, 0.01f);
        // Actualizamos el vector de velocidad.
        _velocity = Vector3.MoveTowards(_velocity,desiredVelocity,movementRate * Time.fixedDeltaTime);

        // Sincronizamos la velocidad escalar con el movimiento real.
        if (hasMovementDirection) _currentSpeed = Vector3.Dot(_velocity, movementDirection);
        else
        {
            _currentSpeed = _velocity.magnitude;
            if (_velocity.sqrMagnitude < 0.0025f) _currentSpeed = 0f;
        }
        _rb.MovePosition(_rb.position + _velocity * Time.fixedDeltaTime);
        CheckEmergencyBrakeRelease();
    }
    private void CheckEmergencyBrakeRelease()
    {
        if (!_isEmergencyBraking)
            return;

        if (_velocity.magnitude > 0.05f ||
            Mathf.Abs(_currentSpeed) > 0.05f)
            return;

        _velocity = Vector3.zero;
        _currentSpeed = 0f;

        ReleaseEmergencyBrake();

        Debug.Log("[SubmarineMovement] Emergency brake released.");
    }

    private void UpdateRotation()
    {
        if (!_hasTarget || _currentWaypoints == null || _currentIndex >= _currentWaypoints.Count) return;
        if (_velocity.sqrMagnitude < 0.0001f) return;
        Vector3 direction = _currentWaypoints[_currentIndex] - _rb.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;
        direction.Normalize();
        direction = Quaternion.Euler(0f, _currentAngle, 0f) * direction;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        Quaternion newRotation = Quaternion.Slerp(_rb.rotation,targetRotation,Time.fixedDeltaTime / Mathf.Max(rotationSmoothTime, 0.01f));
        _rb.MoveRotation(newRotation);
    }

    #endregion

    #region Waypoints

    private void GetNewWaypointList(OnSubmarineRouteChanged data)
    {
        _currentWaypoints = new List<Vector3>(data._waypoints);
        _currentIndex = 0;
        _hasTarget = _currentWaypoints.Count > 0;
    }

    private void CheckWaypointArrival()
    {
        if (!_hasTarget || _currentWaypoints == null ||
            _currentIndex >= _currentWaypoints.Count)
            return;

        Vector3 target = _currentWaypoints[_currentIndex];

        // Trabajamos únicamente sobre el plano XZ.
        Vector3 previousPosition = new Vector3(
            _previousPosition.x, 0f, _previousPosition.z);

        Vector3 currentPosition = new Vector3(
            _rb.position.x, 0f, _rb.position.z);

        target.y = 0f;

        // Comprobamos si terminó dentro del radio del waypoint.
        if (Vector3.Distance(currentPosition, target) <= distanceOffset)
        {
            OnWaypoint();
            return;
        }

        // Comprobamos si la trayectoria pasó cerca del waypoint.
        Vector3 movementSegment = currentPosition - previousPosition;

        if (movementSegment.sqrMagnitude < 0.0001f) return;
        float t = Vector3.Dot(target - previousPosition,movementSegment) / movementSegment.sqrMagnitude;

        t = Mathf.Clamp01(t);

        Vector3 closestPoint = previousPosition + movementSegment * t;

        float distanceToSegment = Vector3.Distance(closestPoint, target);

        if (distanceToSegment <= distanceOffset)
            OnWaypoint();
    }

    private void OnWaypoint()
    {
        GameEventChannel<OnSubmarineArrivedAtCheckpoint>.RaiseEvent(new OnSubmarineArrivedAtCheckpoint());
        _currentIndex++;
        if (_currentIndex >= _currentWaypoints.Count)
        {
            _hasTarget = false;
            _targetSpeed = 0f;
            _speedMode = SubmarineSpeedMode.Inactive;
        }
    }

    #endregion

    #region EmergencyBrake

    public bool ActivateEmergencyBrake()
    {
        if (!IsEngineRunning() || _isEmergencyBraking)
            return false;

        _isEmergencyBraking = true;
        _speedMode = SubmarineSpeedMode.Inactive;
        _targetSpeed = 0f;

        return true;
    }

    public void ReleaseEmergencyBrake()
    {
        _isEmergencyBraking = false;
    }

    #endregion

    #region Collision

    private void OnSubmarineCollision(OnSubmarineCollision collision)
    {
        if (_rb == null) return;
        Vector3 closestPoint = collision.subCollider.ClosestPoint(_rb.position);
        Vector3 normal = _rb.position - closestPoint;
        normal = normal.sqrMagnitude > 0.001f ? normal.normalized : -_velocity.normalized;
        float impactSpeed = _velocity.magnitude;
        _hasTarget = false;
        _currentWaypoints.Clear();
        _currentIndex = 0;
        _velocity = Vector3.Reflect(_velocity, normal) * bounceFactor;
        _targetSpeed = 0f;
        _speedMode = SubmarineSpeedMode.Inactive;
        GameEventChannel<OnSubmarineRouteCleared>.RaiseEvent(new OnSubmarineRouteCleared());
        GameEventChannel<OnSubmarineImpact>.RaiseEvent(new OnSubmarineImpact(normal, impactSpeed));
    }

    #endregion
}


