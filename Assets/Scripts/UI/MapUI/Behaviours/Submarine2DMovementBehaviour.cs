using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Submarine2DMovementBehaviour : MonoBehaviour, ISetup
{
    [Header("Properties")]
    [SerializeField] private float smoothTime = 0.3f;
    [SerializeField] private float rotationSmoothTime = 0.15f;
    [SerializeField] private float maxMovementSpeed = 10f;
    [SerializeField] private float maxRotationSpeed = 10f;
    [SerializeField] private float offsetRotation = -90f;
    [SerializeField] private float distanceOffset = 0.1f;
    
    private Action _onWaypointReached;
    private List<RectTransform> _currentWaypointPoints;
    private List<RectTransform> _newWaypointPoints;
    private int _currentIndex;
    private RectTransform _selfTransform;
    private RectTransform _currentTarget;
    private Coroutine _movementCoroutine;
    private float _rotationVelocity = 0f;
    private Vector2 _velocity = Vector2.zero;
    
    public event Action OnWaypointReached;
    public bool IsInitialized { get; }

    private void Start()
    {
        _selfTransform = GetComponent<RectTransform>();
    }
    
    public void Setup() => Setup(smoothTime, rotationSmoothTime, maxMovementSpeed, maxRotationSpeed, offsetRotation, distanceOffset);

    public void Setup(float subSmoothTime, float subRotationSmoothTime, float subMaxMovementSpeed, float subMaxRotationSpeed, float subOffsetRotation, float subDistanceOffset)
    {
        if (IsInitialized) return;
        smoothTime = subSmoothTime;
        rotationSmoothTime = subRotationSmoothTime;
        maxMovementSpeed = subMaxMovementSpeed;
        maxRotationSpeed = subMaxRotationSpeed;
        offsetRotation = subOffsetRotation;
        distanceOffset = subDistanceOffset;
    }
    
    private void OnDisable()
    {
        if (_onWaypointReached != null)
        {
            OnWaypointReached -= _onWaypointReached;
        }
    }

    public void SetWaypointReachedAction(Action waypointReached)
    {
        _onWaypointReached = waypointReached;
        OnWaypointReached += waypointReached;
    }

    #region CoroutinesHandlers

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

    [ContextMenu("Movement/StopMovementTowards")]
    public void StopMovingTowards()
    {
        if (_movementCoroutine != null)
        {
            StopCoroutine(_movementCoroutine);
        }
    }

    #endregion

    #region Coroutines

    private IEnumerator MoveSmoothTowards()
    {
        _velocity = Vector2.zero;
        _rotationVelocity = 0f;

        yield return RotateTowardsTarget();

        while (_currentTarget != null &&
               Vector2.Distance(_selfTransform.anchoredPosition, _currentTarget.anchoredPosition) > distanceOffset)
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

        CheckTargetAvailability();
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

    #endregion

    #region TargetTools

    private void GetCurrentTarget()
    {
        if (_currentIndex > _currentWaypointPoints.Count - 1) return;
        _currentTarget = _currentWaypointPoints[_currentIndex];
        _currentIndex++;
    }

    private void CheckTargetAvailability()
    {
        if (_currentTarget == null)
        {
            UpdateToNewWaypointList();
        }
        else
        {
            _velocity = Vector2.zero;
            _selfTransform.anchoredPosition = _currentTarget.anchoredPosition;
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

    public void GetNewWaypointList(List<RectTransform> waypoints)
    {
        _newWaypointPoints = waypoints;
    }

    public void UpdateToNewWaypointList()
    {
        if (_newWaypointPoints.Count > 0)
        {
            if (_currentWaypointPoints != _newWaypointPoints)
            {
                _currentWaypointPoints = _newWaypointPoints;
                if (_currentWaypointPoints[0] != _currentTarget)
                {
                    _currentIndex = 0;
                }
            }
        }

        OnStartMovingTowards();
    }

    #endregion
}