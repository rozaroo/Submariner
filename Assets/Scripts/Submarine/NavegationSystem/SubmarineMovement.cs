using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SubmarineMovement : MonoBehaviour
{
    [Header("Movement Properties")]
    [SerializeField] private float smoothTime = 0.9f;
    [SerializeField] private float rotationSmoothTime = 0.15f;
    [SerializeField] private float maxMovementSpeed = 20f;
    [SerializeField] private float maxRotationSpeed = 40f;
    [SerializeField] private float distanceOffset = 0.1f;
    [SerializeField] private float smoothDeaccelerationTime = 1f;

    [Header("Collision Properties")] 
    [SerializeField] private float _bounceFactor = 0.4f;
    
    private Rigidbody _rb;
    private List<Vector3> _currentWaypoints;
    private List<Vector3> _newWaypoints;
    private bool _hasTarget;
    private int _currentIndex;
    private Vector3 _currentTarget;
    private Vector3 _velocity = Vector3.zero;
    private Coroutine _movementCoroutine;
    
    private float _rotationVelocity = 0f;
    private float _speedMultiplier = 1f;
    private EngineState _currentEngineState = EngineState.Off;
    
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

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _currentWaypoints = new List<Vector3>();
        _newWaypoints = new List<Vector3>();
    }

    #region EngineControlCheck

    private void HandleEngineStateChanged(OnEngineStateChanged data)
    {
        _currentEngineState = data.State;
        _speedMultiplier = data.SpeedMultiplier;

        if (IsEngineRunning())
        {
            if (_newWaypoints != null && _newWaypoints.Count > 0 && !_hasTarget)
            {
                UpdateToNewWaypointList();
            }
        }
        else
        {
            StopMovingTowards();
        }
    }
    
    private bool IsEngineRunning()
    {
        return _currentEngineState == EngineState.Operative || _currentEngineState == EngineState.Degraded;
    }

    #endregion

    private void OnSubmarineCollision(OnSubmarineCollision collision)
    {
        Vector3 closestPoint = collision.subCollider.ClosestPoint(_rb.position);
        Vector3 normal = _rb.position - closestPoint;

        normal = normal.sqrMagnitude > 0.001f ? normal.normalized : -_velocity.normalized;
        float impactSpeed = _velocity.magnitude;
        
        _hasTarget = false;
        _currentWaypoints?.Clear();
        _newWaypoints?.Clear();

        if (_movementCoroutine != null) StopCoroutine(_movementCoroutine);
        _velocity = Vector3.Reflect(_velocity, normal) * _bounceFactor;
        _movementCoroutine = StartCoroutine(DecelerateToStop());

        GameEventChannel<OnSubmarineRouteCleared>.RaiseEvent(new OnSubmarineRouteCleared());
        GameEventChannel<OnSubmarineImpact>.RaiseEvent(new OnSubmarineImpact(normal, impactSpeed));
    }

    #region CoroutinesHandlers

    [ContextMenu("Movement/StartMovementTowards")]
    public void StartMovingTowards()
    {
        if (_movementCoroutine != null) StopCoroutine(_movementCoroutine);
        GetCurrentTarget();
        if (_hasTarget) _movementCoroutine = StartCoroutine(MoveSmoothTowards());
    }

    private void StopMovingTowards()
    {
        if (_movementCoroutine != null)
            StopCoroutine(_movementCoroutine);
        
        _movementCoroutine = StartCoroutine(DecelerateToStop());
    }
    
    #endregion

    #region Coroutines

    private IEnumerator MoveSmoothTowards()
    {
        _velocity = Vector3.zero;
        _rotationVelocity = 0f;

        yield return RotateTowardsTarget();
    
        float sqrDistanceOffset = distanceOffset * distanceOffset;

        while (_hasTarget && (_currentTarget - _rb.position).sqrMagnitude > sqrDistanceOffset)
        {
            _rb.MovePosition(Vector3.SmoothDamp(_rb.position, _currentTarget, ref _velocity, smoothTime, maxMovementSpeed * _speedMultiplier));
            yield return new WaitForFixedUpdate();
        }
        _velocity = Vector3.zero; 
        CheckTargetAvailability();
    }

    private IEnumerator RotateTowardsTarget()
    {
        while (_hasTarget)
        {
            Vector3 dir = _currentTarget - _rb.position;
            dir.y = 0;

            if (dir.sqrMagnitude < 0.001f) yield break;

            float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float currentAngle = _rb.rotation.eulerAngles.y;

            float smoothAngle = Mathf.SmoothDampAngle(
                currentAngle, 
                targetAngle,
                ref _rotationVelocity,
                rotationSmoothTime, 
                maxRotationSpeed);

            _rb.MoveRotation(Quaternion.Euler(
                _rb.rotation.eulerAngles.x, 
                smoothAngle, 
                _rb.rotation.eulerAngles.z));

            if (Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle)) < 1f)
            {
                _rotationVelocity = 0f;
                yield break;
            }

            yield return new WaitForFixedUpdate();
        }
    }
    
    private IEnumerator DecelerateToStop()
    {
        Vector3 velocityDampRef = Vector3.zero;
        float rotationDampRef = 0f;
        
        while (_velocity.magnitude > 0.01f || Mathf.Abs(_rotationVelocity) > 0.01f)
        {
            _velocity = Vector3.SmoothDamp(
                _velocity, 
                Vector3.zero, 
                ref velocityDampRef, 
                smoothDeaccelerationTime, 
                maxMovementSpeed);
        
            _rotationVelocity = Mathf.SmoothDamp(
                _rotationVelocity, 
                0f, 
                ref rotationDampRef, 
                smoothDeaccelerationTime, 
                maxRotationSpeed);
            
            _rb.MovePosition(_rb.position + _velocity * Time.fixedDeltaTime);
            
            Vector3 currentLocalEuler = _rb.rotation.eulerAngles;
            float newYAngle = currentLocalEuler.y + (_rotationVelocity * Time.fixedDeltaTime);

            _rb.MoveRotation(Quaternion.Euler(currentLocalEuler.x, newYAngle, currentLocalEuler.z));
            
            yield return new WaitForFixedUpdate();
        }
        _velocity = Vector3.zero;
        _rotationVelocity = 0f;
    }

    #endregion

    #region TargetTools

    private void GetCurrentTarget()
    {
        if (_currentIndex > _currentWaypoints.Count - 1)
        {
            _hasTarget = false;
            return;
        }
        _currentTarget = _currentWaypoints[_currentIndex];
        _hasTarget = true;
        _currentIndex++;
    }

    private void CheckTargetAvailability()
    {
        if (!_hasTarget)
        {
            UpdateToNewWaypointList();
        }
        else
        {
            _velocity = Vector3.zero;
            _rb.position = _currentTarget;
            OnWaypoint();
        }
    }

    #endregion

    #region WaypointUpdates

    private void OnWaypoint()
    {
        GameEventChannel<OnSubmarineArrivedAtCheckpoint>.RaiseEvent(new OnSubmarineArrivedAtCheckpoint());
        UpdateToNewWaypointList();
    }

    private void GetNewWaypointList(OnSubmarineRouteChanged data)
    {
        _newWaypoints = data._waypoints;
        UpdateToNewWaypointList();
    }

    private void UpdateToNewWaypointList()
    {
        if (_newWaypoints != null && _newWaypoints.Count > 0)
        {
            _currentWaypoints = _newWaypoints;
            
            if (!_hasTarget || Vector3.Distance(_currentWaypoints[0], _currentTarget) > 0.05f)
            {
                _currentIndex = 0;
                StartMovingTowards();
            }
        }
        else
        {
            _currentWaypoints?.Clear();
            _hasTarget = false;
            StopMovingTowards();
        }
    }

    #endregion
}

