using Bitgem.VFX.StylisedWater;
using UnityEngine;

public class FloodSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform waterMesh;
    [SerializeField] private WaterVolumeBox waterVolume;

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
    private Renderer _volumeRenderer;
    private Vector3 _volumeScale;
    private Vector3 _volumeTop;
    private float _volumeDepth;
    private float _surfaceOffset;

    private void Awake()
    {
        // Nos suscribimos en Awake para que el script escuche los eventos 
        // INCLUSO si está apagado (enabled = false).
        GameEventChannel<OnHullPropertyChange>.OnEventRaised += OnHullStatusChanged;
        GameEventChannel<OnDrainagePropertyChange>.OnEventRaised += OnDrainageStatusReceived;
    }

    private void Start()
    {
        if (waterVolume != null)
        {
            waterVolume.Rebuild();

            Bounds bounds = waterVolume.GetComponent<MeshFilter>().sharedMesh.bounds;
            Transform volume = waterVolume.transform;
            _volumeRenderer = waterVolume.GetComponent<Renderer>();
            _volumeScale = volume.localScale;
            _volumeTop = new Vector3(0f, bounds.max.y, 0f);
            _volumeDepth = volume.TransformVector(Vector3.up * bounds.size.y).y;
            _surfaceOffset = volume.TransformPoint(_volumeTop).y - waterMesh.position.y;
        }

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

        if (_volumeRenderer == null) return;

        float depth = y - startHeight;
        _volumeRenderer.enabled = depth > 0f;
        if (depth <= 0f) return;

        Transform volume = waterVolume.transform;
        Vector3 scale = _volumeScale;
        scale.y *= depth / _volumeDepth;
        volume.localScale = scale;

        // Preserve the existing trigger motion while the visible volume grows from the dry waterline.
        Vector3 volumePosition = volume.position;
        volumePosition.y += y + _surfaceOffset - volume.TransformPoint(_volumeTop).y;
        volume.position = volumePosition;
    }
}