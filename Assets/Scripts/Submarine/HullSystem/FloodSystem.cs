using System;
using UnityEngine;

public class FloodSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform waterMesh;

    [Header("Settings")]
    [SerializeField] private float maxRiseSpeed = 1f;
    [SerializeField] private float startHeight = 0f;
    [SerializeField] private float maxHeight = 10f;

    private float _hullFloodingSpeed;
    private float _drainageSpeed;
    private float EffectiveFloodingSpeed => _hullFloodingSpeed - _drainageSpeed;
    private float _currentHeight;
    private bool _sunkLogged;
    private bool _isFlooding;
    
    private void Start()
    {
        _currentHeight = startHeight;
        SetWaterHeight(_currentHeight);
    }

    private void OnEnable()
    {
        GameEventChannel<OnHullPropertyChange>.OnEventRaised += OnHullStatusChanged;
        GameEventChannel<OnDrainagePropertyChange>.OnEventRaised += OnDrainageStatusReceived;
    }

    private void OnDisable()
    {
        GameEventChannel<OnHullPropertyChange>.OnEventRaised -= OnHullStatusChanged;
        GameEventChannel<OnDrainagePropertyChange>.OnEventRaised -= OnDrainageStatusReceived;
    }

    private void Update()
    {
        if (_currentHeight <= startHeight && EffectiveFloodingSpeed <= 0)
        {
            enabled = false;
            return;
        }

        _currentHeight = Mathf.Clamp(_currentHeight + EffectiveFloodingSpeed * Time.deltaTime, startHeight, maxHeight);
        SetWaterHeight(_currentHeight);
        CheckProgress();
    }

    private void OnHullStatusChanged(OnHullPropertyChange onHullPropertyChange)
    {
        if (onHullPropertyChange.activeHullDamage <= 0)
        {
            _hullFloodingSpeed = 0f;
            _isFlooding = false;
            SFXManager.PostEvent("Stop_TensionEvent", gameObject);
            return;
        }

        enabled = true;
        
        float damageRatio = onHullPropertyChange.activeHullDamage / onHullPropertyChange.maxHullDamagePosible;
        _hullFloodingSpeed = maxRiseSpeed * damageRatio;

        if (!_isFlooding)
        {        
            _isFlooding = true;
            SFXManager.PostEvent("Start_TensionEvent", gameObject);
        }
    }

    private void OnDrainageStatusReceived(OnDrainagePropertyChange onDrainagePropertyChange)
    {
        _drainageSpeed = maxRiseSpeed * onDrainagePropertyChange.drainagePercentage;
    }
    
    private void CheckProgress()
    {
        if (_sunkLogged) return;
        float progress = (_currentHeight - startHeight) / (maxHeight - startHeight);
        SFXManager.SetRtpcValue("IncrementalTension", progress);
        if (progress >= 0.8f)
        {
            Log.Info("Submarine Sunk");
            GameEventChannel<OnDeath>.RaiseEvent(new OnDeath(DeathType.SubmarineSunk));
            _sunkLogged = true;
        }
    }

    private void SetWaterHeight(float y)
    {
        Vector3 pos = waterMesh.position;
        pos.y = y;
        waterMesh.position = pos;
    }
   
}
