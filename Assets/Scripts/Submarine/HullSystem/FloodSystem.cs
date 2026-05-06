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
    private bool _sunkLogged;

    private void Start()
    {
        _currentHeight = startHeight;
        SetWaterHeight(_currentHeight);
    }

    private void OnEnable()
    {
        onHullStatusChanged.OnEventRaised += OnHullStatusChanged;
        onDrainageStationStatusChanged.OnEventRaised += OnDrainageStatusReceived;
    }

    private void OnDisable()
    {
        onHullStatusChanged.OnEventRaised -= OnHullStatusChanged;
        onDrainageStationStatusChanged.OnEventRaised -= OnDrainageStatusReceived;
    }

    private void Update()
    {
        if (EffectiveFloodingSpeed == 0) return;

        _currentHeight = Mathf.Clamp(_currentHeight + EffectiveFloodingSpeed * Time.deltaTime, startHeight, maxHeight);
        SetWaterHeight(_currentHeight);
        CheckProgress();
    }

    private void OnHullStatusChanged(HullProperty hullProperty)
    {
        if (hullProperty.activeHullDamage <= 0)
        {
            _hullFloodingSpeed = 0f;
            _drainageSpeed = 0f;
            return;
        }
        _hullFloodingSpeed = maxRiseSpeed * (hullProperty.activeHullDamage / hullProperty.maxHullDamagePosible);
    }

    private void OnDrainageStatusReceived(DrainagePropertyData drainagePropertyData)
    {
        _drainageSpeed = maxRiseSpeed * drainagePropertyData.drainagePercentage;
    }



    private void CheckProgress()
    {
        if (_sunkLogged) return;
        float progress = (_currentHeight - startHeight) / (maxHeight - startHeight);
        if (progress >= 0.7f)
        {
            onSubmarineSunk?.RaiseEvent();
            _sunkLogged = true;
        }
    }

    private void SetWaterHeight(float y)
    {
        Vector3 pos = waterMesh.position;
        pos.y = y;
        waterMesh.position = pos;
    }
    public void StopDrainage()
    {
        _drainageSpeed = 0f;
        Debug.Log("Drenaje apagado manualmente");
    }
}
