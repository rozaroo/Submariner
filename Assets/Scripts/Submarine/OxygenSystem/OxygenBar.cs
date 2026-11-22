using UnityEngine;

public class OxygenBar : MonoBehaviour
{
    private static readonly int OxygenFillId = Shader.PropertyToID("_OxygenFill");

    private Transform _barTransform;
    private Vector3 _originalScale;
    private Vector3 _originalPosition;
    private Renderer _fillRenderer;
    private MaterialPropertyBlock _fillProperties;
    private OxygenSystem _oxygenSystem;
    private float _lastMaterialFill = -1f;
    private bool _scaledAsLegacyBar;

    private void Awake()
    {
        _barTransform = transform;
        _originalScale = _barTransform.localScale;
        _originalPosition = _barTransform.localPosition;

        if (TryGetComponent(out _fillRenderer))
        {
            _fillProperties = new MaterialPropertyBlock();
        }
    }

    private void OnEnable()
    {
        _oxygenSystem = FindFirstObjectByType<OxygenSystem>();
        _lastMaterialFill = -1f;
        GameEventChannel<OnOxygenChanged>.OnEventRaised += UpdateBar;
    }

    private void OnDisable()
    {
        GameEventChannel<OnOxygenChanged>.OnEventRaised -= UpdateBar;
    }

    private void LateUpdate()
    {
        if (_oxygenSystem == null) return;

        // The drain runs every frame; the shared event only publishes periodic snapshots.
        TrySetMaterialFill(GetRatio(_oxygenSystem.CurrentOxygen, _oxygenSystem.MaxOxygen));
    }

    private static float GetRatio(float current, float maximum)
    {
        return maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
    }

    private bool TrySetMaterialFill(float ratio)
    {
        Material material = _fillRenderer != null ? _fillRenderer.sharedMaterial : null;
        if (material == null || !material.HasProperty(OxygenFillId)) return false;

        if (_scaledAsLegacyBar)
        {
            _barTransform.localScale = _originalScale;
            _barTransform.localPosition = _originalPosition;
            _scaledAsLegacyBar = false;
        }
        if (_lastMaterialFill != ratio)
        {
            _fillRenderer.GetPropertyBlock(_fillProperties);
            _fillProperties.SetFloat(OxygenFillId, ratio);
            _fillRenderer.SetPropertyBlock(_fillProperties);
            _lastMaterialFill = ratio;
        }
        return true;
    }

    private void UpdateBar(OnOxygenChanged oxygenProperty)
    {
        float ratio = GetRatio(oxygenProperty.currentOxygen, oxygenProperty.maxOxygen);
        if (TrySetMaterialFill(ratio)) return;

        _scaledAsLegacyBar = true;
        _barTransform.localScale = new Vector3(
            _originalScale.x * ratio,
            _originalScale.y,
            _originalScale.z
        );

        _barTransform.localPosition = new Vector3(
            _originalPosition.x - (_originalScale.x * (1f - ratio)) / 2f,
            _originalPosition.y,
            _originalPosition.z
        );
    }
}
