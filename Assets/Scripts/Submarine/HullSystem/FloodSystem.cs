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

    private void Awake()
    {
        // Nos suscribimos en Awake para que el script escuche los eventos 
        // INCLUSO si está apagado (enabled = false).
        GameEventChannel<OnHullPropertyChange>.OnEventRaised += OnHullStatusChanged;
        GameEventChannel<OnDrainagePropertyChange>.OnEventRaised += OnDrainageStatusReceived;
    }

    private void Start()
    {
        _currentHeight = startHeight;
        SetWaterHeight(_currentHeight);
    }

    private void OnDestroy()
    {
        // Nos desuscribimos solo cuando el objeto es destruido (ej: cambio de escena)
        GameEventChannel<OnHullPropertyChange>.OnEventRaised -= OnHullStatusChanged;
        GameEventChannel<OnDrainagePropertyChange>.OnEventRaised -= OnDrainageStatusReceived;
    }

    private void Update()
    {
        if (_currentHeight <= startHeight && EffectiveFloodingSpeed <= 0)
        {
            // Ahora esto es 100% seguro. Apaga el Update pero no los Eventos.
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

        // Si hay daño, volvemos a encender el Update para que el agua suba
        enabled = true;

        float damageRatio = onHullPropertyChange.activeHullDamage / (float)onHullPropertyChange.maxHullDamagePosible;
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