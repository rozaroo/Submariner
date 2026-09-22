using UnityEngine;

public class LightObject : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Light lightComponent;
    [SerializeField] private Color lightColor = Color.white;
    [Range(0f, 10f)]
    [SerializeField] private float intensity = 0.01f;

    [Header("Material Settings")]
    [Tooltip("La intensidad de emisi�n base del material. Pon aqu� el mismo valor que tienes en tu shader (ej. 1).")]
    [SerializeField] private float materialEmissionIntensity = 1f;

    private MaterialPropertyBlock _lightMaterialPropertyBlock;
    private bool _isToggledOn = true;
    
    private Color _initialLightColor;
    private float _initialLightIntensity;
    private Color _baseMatLightColor;
    private Color _baseMatEmissionColor;

    public Color LightColor => lightColor;
    public float Intensity => intensity;

    public void Initialize()
    {
        _lightMaterialPropertyBlock = new MaterialPropertyBlock();

        if (lightComponent != null)
        {
            _isToggledOn = lightComponent.enabled;
            
            lightColor = lightComponent.color;
            intensity = lightComponent.intensity;

            _initialLightColor = lightComponent.color;
            _initialLightIntensity = lightComponent.intensity;
        }

        if (targetRenderer != null)
        {
            _baseMatLightColor = targetRenderer.sharedMaterial.GetColor("_LightColor");
            _baseMatEmissionColor = targetRenderer.sharedMaterial.GetColor("_EmissionColor");
        }

        ApplySettings();
    }

    private void ApplySettings()
    {
        if (lightComponent != null)
        {
            lightComponent.color = lightColor;
            lightComponent.intensity = _isToggledOn ? intensity : 0f;
        }

        if (targetRenderer != null)
        {
            targetRenderer.GetPropertyBlock(_lightMaterialPropertyBlock);

            
            if (lightColor == _initialLightColor)
            {
                _lightMaterialPropertyBlock.SetColor("_LightColor", _baseMatLightColor);
                _lightMaterialPropertyBlock.SetColor("_EmissionColor", _baseMatEmissionColor);
            }
            else
            {
               
                _lightMaterialPropertyBlock.SetColor("_LightColor", lightColor);
                _lightMaterialPropertyBlock.SetColor("_EmissionColor", lightColor);
            }

            
            float intensityRatio = 1f;
            if (_initialLightIntensity > 0.001f)
            {
               
                intensityRatio = intensity / _initialLightIntensity;
            }

            
            float currentEmission = _isToggledOn ? (materialEmissionIntensity * intensityRatio) : 0f;

            _lightMaterialPropertyBlock.SetFloat("_EmissionIntensity", currentEmission);

            targetRenderer.SetPropertyBlock(_lightMaterialPropertyBlock);
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