using System.Collections.Generic;
using UnityEngine;

public class LightObject : MonoBehaviour
{
    private static readonly int LightColorId = Shader.PropertyToID("_LightColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int EmissionIntensityId = Shader.PropertyToID("_EmissionIntensity");

    [Header("Settings")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Light lightComponent;
    [SerializeField] private Color lightColor = Color.white;
    [Range(0f, 10f)]
    [SerializeField] private float intensity = 0.01f;

    [Header("Material Settings")]
    [Tooltip("La intensidad de emisi�n base del material. Pon aqu� el mismo valor que tienes en tu shader (ej. 1).")]
    [SerializeField] private float materialEmissionIntensity = 1f;

    private readonly List<Material> _sharedMaterials = new();
    private MaterialPropertyBlock _lightMaterialPropertyBlock;
    private bool _isToggledOn = true;
    private Color _initialLightColor;
    private float _initialLightIntensity;

    public Color LightColor => lightColor;
    public float Intensity => intensity;

    public void Initialize()
    {
        if (lightComponent != null)
        {
            _isToggledOn = lightComponent.enabled;
            lightColor = lightComponent.color;
            intensity = lightComponent.intensity;
        }

        _initialLightColor = lightColor;
        _initialLightIntensity = intensity;
        ApplySettings();
    }

    private void ApplySettings()
    {
        if (lightComponent != null)
        {
            lightComponent.color = lightColor;
            lightComponent.intensity = _isToggledOn ? intensity : 0f;
        }

        if (targetRenderer == null) return;

        bool useBaseColors = lightColor == _initialLightColor;
        float intensityRatio = _initialLightIntensity > 0.001f ? intensity / _initialLightIntensity : 1f;
        float currentEmission = _isToggledOn ? materialEmissionIntensity * intensityRatio : 0f;

        targetRenderer.GetSharedMaterials(_sharedMaterials);
        for (int index = 0; index < _sharedMaterials.Count; index++)
        {
            Material material = _sharedMaterials[index];
            if (material == null) continue;

            // Station lights share a renderer with the copper housing.
            bool hasLightColor = material.HasColor(LightColorId);
            bool hasEmissionColor = material.HasColor(EmissionColorId);
            bool hasEmissionIntensity = material.HasFloat(EmissionIntensityId);
            if (!hasLightColor && !hasEmissionColor && !hasEmissionIntensity) continue;

            _lightMaterialPropertyBlock ??= new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(_lightMaterialPropertyBlock, index);
            if (_lightMaterialPropertyBlock.isEmpty) targetRenderer.GetPropertyBlock(_lightMaterialPropertyBlock);

            if (hasLightColor)
                _lightMaterialPropertyBlock.SetColor(LightColorId, useBaseColors ? material.GetColor(LightColorId) : lightColor);
            if (hasEmissionColor)
                _lightMaterialPropertyBlock.SetColor(EmissionColorId, useBaseColors ? material.GetColor(EmissionColorId) : lightColor);
            if (hasEmissionIntensity)
                _lightMaterialPropertyBlock.SetFloat(EmissionIntensityId, currentEmission);

            targetRenderer.SetPropertyBlock(_lightMaterialPropertyBlock, index);
        }
    }

    public void SetColor(Color newColor)
    {
        lightColor = newColor;
        ApplySettings();
    }

    public void SetIntensity(float newIntensity)
    {
        intensity = newIntensity;
        ApplySettings();
    }

    public void Toggle(bool lightEnabled)
    {
        _isToggledOn = lightEnabled;
        if (lightComponent != null) lightComponent.enabled = lightEnabled;
        ApplySettings();
    }
}