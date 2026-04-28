using System.Collections;
using UnityEngine;

public class FloodSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform waterMesh;

    [Header("Settings")]
    [SerializeField] private float maxRiseSpeed = 1f;
    [SerializeField] private float startHeight = 0f;
    [SerializeField] private float maxHeight = 10f;
    
    [Header("Event Channels")]
    [SerializeField] private HullPropertyEventSO onHullStatusChanged;
    [SerializeField] private DrainagePropertyEventChannelSO onDrainageStationStatusChanged;
    [SerializeField] private BaseEventChannelSO onSubmarineSunk;
    
    private float _hullFloodingSpeed;
    private float _drainageSpeed;
    private float EffectiveFloodingSpeed => _hullFloodingSpeed - _drainageSpeed;
    private float _currentHeight;
    private bool _halfwayLogged;
    private Coroutine _floodingCoroutine;

    private void Start()
    {
        _currentHeight = startHeight;
        SetWaterHeight(_currentHeight);
    }
    private void OnEnable()
    {
        onDrainageStationStatusChanged.OnEventRaised += OnDrainageStatusReceived;
        onHullStatusChanged.OnEventRaised += ChangeFloodingSpeed;
    }
    
    private void OnDisable()
    {
        onDrainageStationStatusChanged.OnEventRaised -= OnDrainageStatusReceived;
        onHullStatusChanged.OnEventRaised -= ChangeFloodingSpeed;
    }
    
    private void ChangeFloodingSpeed(HullProperty hullProperty)
    {
        if (hullProperty.activeHullDamage <= 0)
        {
            if (_floodingCoroutine != null) StopCoroutine(_floodingCoroutine);
            _floodingCoroutine = null;
            _hullFloodingSpeed = 0f;
            return; 
        }
        _hullFloodingSpeed = maxRiseSpeed * (hullProperty.activeHullDamage / hullProperty.maxHullDamagePosible);
        StartFloodingCoroutine(EffectiveFloodingSpeed);
    }

    private void StartFloodingCoroutine(float baseFloodingSpeed)
    {
        if (_floodingCoroutine != null)
        {
            StopCoroutine(_floodingCoroutine);
        }
        _floodingCoroutine = StartCoroutine(FloodingRoutine(baseFloodingSpeed));
    }

    private IEnumerator FloodingRoutine(float floodingSpeed)
    {
        while (_currentHeight < maxHeight)
        {
            _currentHeight = Mathf.Clamp(_currentHeight + floodingSpeed * Time.deltaTime, startHeight, maxHeight);
            CheckProgress();
            SetWaterHeight(_currentHeight);
            yield return null;
        }
    }

    private void CheckProgress()
    {
        var progress = (_currentHeight - startHeight) / (maxHeight - startHeight);
        if (_halfwayLogged || !(progress >= 0.7f)) return;
        onSubmarineSunk?.RaiseEvent();
        _halfwayLogged = true;
    }

    private void OnDrainageStatusReceived(DrainagePropertyData drainagePropertyData)
    {
        _drainageSpeed = maxRiseSpeed * drainagePropertyData.drainagePercentage;
        StartFloodingCoroutine(EffectiveFloodingSpeed);
    }
    
    private void SetWaterHeight(float y)
    {
        Vector3 pos = waterMesh.position;
        pos.y = y;
        waterMesh.position = pos;
    }
}
