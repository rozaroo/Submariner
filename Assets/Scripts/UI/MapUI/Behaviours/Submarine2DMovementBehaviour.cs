using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Submarine2DMovementBehaviour : MonoBehaviour
{
    [SerializeField] private float smoothTime = 0.3f;
    [SerializeField] private float rotationSmoothTime = 0.15f;
    [SerializeField] private float maxMovementSpeed = 10f;
    [SerializeField] private float maxRotationSpeed = 10f;
    [SerializeField] private float offsetRotation= 90f;
    [SerializeField] private float distanceOffset = 0.1f;
    
    public event Action OnWaypointReached;

    private List<RectTransform> waypointPoints;
    private int _currentIndex;
    private RectTransform _selfTransform;
    private RectTransform _currentTarget;

    private Coroutine _movementCoroutine;
    private float _rotationVelocity = 0f;
    private Vector2 _velocity = Vector2.zero;

    private void Start()
    {
        _selfTransform = GetComponent<RectTransform>();
    }
    
    [ContextMenu("Movement/StartMovementTowards")]
    private void OnStartMovingTowards()
    {
        if (_movementCoroutine != null)
        {
            StopCoroutine(_movementCoroutine);
        }
        GetCurrentTarget();
        if (_currentTarget != null)
            _movementCoroutine = StartCoroutine(MoveSmoothTowards());
    }
    
    private void GetCurrentTarget()
    {
        if (waypointPoints.Count > 0)
        {
            _currentTarget = waypointPoints[_currentIndex];
        }
    }
    
    [ContextMenu("Movement/StopMovementTowards")]
    public void StopMovingTowards()
    {
        if (_movementCoroutine != null)
        {
            StopCoroutine(_movementCoroutine);
        }
    }
    private IEnumerator MoveSmoothTowards()
    {
        _velocity = Vector2.zero;
        _rotationVelocity = 0f;
        
        yield return RotateTowardsTarget();
        
        while (Vector2.Distance(_selfTransform.anchoredPosition, _currentTarget.anchoredPosition) > distanceOffset && _currentTarget != null)
        {
            _selfTransform.anchoredPosition = Vector2.SmoothDamp(
                _selfTransform.anchoredPosition,
                _currentTarget.anchoredPosition,
                ref _velocity,
                smoothTime,
                maxMovementSpeed
            );
            yield return null;
        }

        if (_currentTarget != null)
        {
            _velocity = Vector2.zero;
            _selfTransform.anchoredPosition = _currentTarget.anchoredPosition;
        }
        OnWaypoint();
    }
    
    IEnumerator RotateTowardsTarget()
    {
        while (_currentTarget != null)
        {
            Vector2 dir = _currentTarget.anchoredPosition - _selfTransform.anchoredPosition;
            float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + offsetRotation;

            float currentAngle = _selfTransform.localEulerAngles.z;
            if (currentAngle > 180f) currentAngle -= 360f;

            float smoothAngle = Mathf.SmoothDampAngle(
                currentAngle,
                targetAngle,
                ref _rotationVelocity,
                rotationSmoothTime, 
                maxRotationSpeed
            );
            
            _selfTransform.localRotation = Quaternion.Euler(
                _selfTransform.localEulerAngles.x,
                _selfTransform.localEulerAngles.y,
                smoothAngle
            );

            if (Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle)) < 1f)
                yield break;

            yield return null;
        }
    }
    
    private void OnWaypoint()
    {
        _currentIndex++;
        if (_currentIndex < waypointPoints.Count)
        {
            OnStartMovingTowards();
        }
        OnWaypointReached?.Invoke();
    }
    
    public void OnUpdateWaypointsList(List<RectTransform> waypoints)
    {
        waypointPoints = waypoints;
        if (waypointPoints.Count > 0)
        {
            _currentIndex = 0;
            OnStartMovingTowards();
        }
        else
        {
            StopMovingTowards();
        }
    }
}
