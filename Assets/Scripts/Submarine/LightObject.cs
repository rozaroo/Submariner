using UnityEngine;

public class LightObject : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Light lightComponent;
    [SerializeField] private Color lightColor = Color.white;
    [Range(0f, 10f)]
    [SerializeField] private float intensity = 0.1f;
    private MaterialPropertyBlock _lightMaterialPropertyBlock;

    public void Initialize()
    {
        _lightMaterialPropertyBlock = new MaterialPropertyBlock();
        ApplySettings();
    }
    
    private void ApplySettings()
    {
        if (lightComponent != null)
        {
            lightComponent.color = lightColor;
            lightComponent.intensity = intensity;
        }
        
        if (targetRenderer != null)
        {
            targetRenderer.GetPropertyBlock(_lightMaterialPropertyBlock);
            _lightMaterialPropertyBlock.SetColor("_LightColor", lightColor);
            _lightMaterialPropertyBlock.SetColor("_EmissionColor", lightColor * intensity);
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

    public void Toggle(bool enabled)
    {
        lightComponent.enabled = enabled;
    }
}
