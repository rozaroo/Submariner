using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubmarineMovement : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] private float smoothTime = 0.9f;
    [SerializeField] private float rotationSmoothTime = 0.15f;
    [SerializeField] private float maxMovementSpeed = 20f;
    [SerializeField] private float maxRotationSpeed = 40f;
    [SerializeField] private float distanceOffset = 0.1f;
    [SerializeField] private float smoothDeaccelerationTime = 1f;
    
    private List<Vector3> _currentWaypoints;
    private List<Vector3> _newWaypoints;
    private bool _hasTarget;
    private int _currentIndex;
    private Transform _selfTransform;
    private Vector3 _currentTarget;
    private Coroutine _movementCoroutine;
    private float _rotationVelocity = 0f;
    private Vector3 _velocity = Vector3.zero;
    
    public event Action OnWaypointReached;

    private void Start()
    {
        _selfTransform = transform;
        _currentWaypoints = new List<Vector3>();
        _newWaypoints = new List<Vector3>();
    }

    #region CoroutinesHandlers

    [ContextMenu("Movement/StartMovementTowards")]
    private void OnStartMovingTowards()
    {
        if (_movementCoroutine != null)
            StopCoroutine(_movementCoroutine);

        GetCurrentTarget();
        if (_hasTarget)
            _movementCoroutine = StartCoroutine(MoveSmoothTowards());
    }

    [ContextMenu("Movement/StopMovementTowards")]
    public void StopMovingTowards()
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

        while (_hasTarget && 
               (_currentTarget - _selfTransform.position).sqrMagnitude > sqrDistanceOffset)
        {
            _selfTransform.position = Vector3.SmoothDamp(
                _selfTransform.position,
                _currentTarget,
                ref _velocity,
                smoothTime,
                maxMovementSpeed
            );
            yield return null;
        }
        _velocity = Vector3.zero; 
        CheckTargetAvailability();
    }

    private IEnumerator RotateTowardsTarget()
    {
        Vector3 brakingVelocityRef = Vector3.zero;
        
        while (_hasTarget)
        {
            if (_velocity.sqrMagnitude > 0.001f)
            {
                _velocity = Vector3.SmoothDamp(_velocity, Vector3.zero, ref brakingVelocityRef, smoothTime, maxMovementSpeed);
                _selfTransform.position += _velocity * Time.deltaTime;
            }
            else
            {
                _velocity = Vector3.zero;
            }
            
            Vector3 dir = _currentTarget - _selfTransform.position;
            dir.y = 0;
            
            if (dir.sqrMagnitude < 0.001f) yield break;
            
            float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

            float currentAngle = _selfTransform.eulerAngles.y;

            float smoothAngle = Mathf.SmoothDampAngle(
                currentAngle, 
                targetAngle,
                ref _rotationVelocity,
                rotationSmoothTime, 
                maxRotationSpeed);

            _selfTransform.rotation = Quaternion.Euler(
                _selfTransform.eulerAngles.x, 
                smoothAngle, 
                _selfTransform.eulerAngles.z);

            if (Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle)) < 1f)
            {
                _rotationVelocity = 0f;
                yield break;
            }

            yield return null;
        }
    }
    
    private IEnumerator DecelerateToStop()
    {
        Vector3 velocityDampRef = Vector3.zero;
        float rotationDampRef = 0f;
        
        while (_velocity.magnitude > 0.01f || Math.Abs(_rotationVelocity) > 0.01f)
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
            
            _selfTransform.position += _velocity * Time.deltaTime;
            
            Vector3 currentLocalEuler = _selfTransform.eulerAngles;
            float newYAngle = currentLocalEuler.y + (_rotationVelocity * Time.deltaTime);
            
            _selfTransform.rotation = Quaternion.Euler(currentLocalEuler.x, newYAngle, currentLocalEuler.z);
            
            yield return null;
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
            _selfTransform.position = _currentTarget;
            OnWaypoint();
        }
    }

    #endregion

    #region WaypointUpdates

    private void OnWaypoint()
    {
        OnWaypointReached?.Invoke();
        UpdateToNewWaypointList();
    }

    public void GetNewWaypointList(List<Vector3> waypoints)
    {
        _newWaypoints = waypoints;
        UpdateToNewWaypointList();
    }

    public void UpdateToNewWaypointList()
    {
        if (_newWaypoints != null && _newWaypoints.Count > 0)
        {
            _currentWaypoints = _newWaypoints;
            
            if (!_hasTarget || Vector3.Distance(_currentWaypoints[0], _currentTarget) > 0.05f)
            {
                _currentIndex = 0;
                OnStartMovingTowards();
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