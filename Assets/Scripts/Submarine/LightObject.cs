using UnityEngine;

public class LightObject : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Light lightComponent;
    [SerializeField] private Color lightColor = Color.white;
    [Range(0f, 10f)]
    [SerializeField] private float intensity = 0.01f;
    private MaterialPropertyBlock _lightMaterialPropertyBlock;

    public void Initialize()
    {
        _lightMaterialPropertyBlock = new MaterialPropertyBlock();
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
            _lightMaterialPropertyBlock.SetFloat("_EmissionIntensity", intensity * 100000f);
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
        lightComponent.enabled = lightEnabled;
        lightColor = lightEnabled ? lightColor : Color.black;
        lightComponent.intensity = lightEnabled ? intensity : 0f;
        ApplySettings();
    }
}
