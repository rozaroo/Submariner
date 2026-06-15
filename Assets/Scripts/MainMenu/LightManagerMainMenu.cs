using UnityEngine;
using System.Collections;

/// <summary>
/// Manager para controlar las luces del Main Menu, permitiendo que hagan fade (se apaguen y prendan suavemente).
/// </summary>
public class LightManagerMainMenu : MonoBehaviour
{
    [Header("Configuración de Luces")]
    [SerializeField] private Light[] lightsToFlicker;
    [SerializeField] private float minIntensity = 0f;
    [Tooltip("Si es 0, usará la intensidad inicial de cada luz.")]
    [SerializeField] private float maxIntensity = 0f; 
    [SerializeField] private float minFadeDuration = 1f;
    [SerializeField] private float maxFadeDuration = 3f;

    private void Start()
    {
        if (lightsToFlicker == null || lightsToFlicker.Length == 0)
        {
            Debug.LogWarning("LightManagerMainMenu: No hay luces asignadas para el fade.");
            return;
        }

        foreach (var light in lightsToFlicker)
        {
            if (light != null)
            {
                light.enabled = true; // Asegurarse de que el componente esté encendido
                float targetMax = maxIntensity > 0f ? maxIntensity : light.intensity;
                StartCoroutine(FadeRoutine(light, targetMax));
            }
        }
    }

    private IEnumerator FadeRoutine(Light light, float targetMaxIntensity)
    {
        // Alternamos entre apagar y encender suavemente
        while (true)
        {
            // Apagar suavemente
            yield return StartCoroutine(FadeTo(light, minIntensity, Random.Range(minFadeDuration, maxFadeDuration)));
            
            // Encender suavemente
            yield return StartCoroutine(FadeTo(light, targetMaxIntensity, Random.Range(minFadeDuration, maxFadeDuration)));
        }
    }

    private IEnumerator FadeTo(Light light, float targetIntensity, float duration)
    {
        float startIntensity = light.intensity;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            light.intensity = Mathf.Lerp(startIntensity, targetIntensity, elapsed / duration);
            yield return null;
        }

        light.intensity = targetIntensity;
    }
}
