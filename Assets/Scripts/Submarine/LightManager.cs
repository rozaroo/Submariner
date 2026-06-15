using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manager centralizado para controlar luces, alertas y desvanecimientos (fades).
/// Fusiona las funcionalidades de SimpleFlicker, FadeManager y el LightManager original.
/// </summary>
public class LightManager : MonoBehaviour
{
    public static LightManager Instance { get; private set; }

    [Header("Configuración de Alertas")]
    [SerializeField] private Light[] lights;
    [SerializeField] private float flickerSpeed = 2f;
    [SerializeField] private float flickerMinIntensity = 0.2f;
    [SerializeField] private float flickerMaxIntensity = 1.5f;
    [SerializeField] private float flickerDuration = 3f;
    [SerializeField] private float alertCycleDuration = 1f;

    private Color _originalColor;
    private float[] _originalIntensities;
    private bool _originalSaved;

    private bool _hullDamageActive;
    private float _lastActiveCrackCount;

    private Coroutine _alertCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        GameEventChannel<OnHullPropertyChange>.OnEventRaised += OnHullStatusChanged;
    }

    private void OnDisable()
    {
        GameEventChannel<OnHullPropertyChange>.OnEventRaised -= OnHullStatusChanged;
    }

    #region Alert Logic (Original LightManager)

    private void OnHullStatusChanged(OnHullPropertyChange onHullPropertyChange)
    {
        bool increased = onHullPropertyChange.activeHullDamage > _lastActiveCrackCount;
        _lastActiveCrackCount = onHullPropertyChange.activeHullDamage;
        _hullDamageActive = onHullPropertyChange.activeHullDamage > 0;

        if (increased) SaveOriginalIfNeeded();
        UpdateAlertState();
    }

    private void SaveOriginalIfNeeded()
    {
        if (_originalSaved || lights == null || lights.Length == 0) return;
        _originalColor = lights[0].color;
        _originalIntensities = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
                _originalIntensities[i] = lights[i].intensity;
        }
        _originalSaved = true;
    }

    private void UpdateAlertState()
    {
        if (_alertCoroutine != null)
        {
            StopCoroutine(_alertCoroutine);
            _alertCoroutine = null;
        }

        if (!_hullDamageActive)
        {
            RestoreLights();
            return;
        }

        _alertCoroutine = StartCoroutine(AlertRoutine());
    }

    private IEnumerator AlertRoutine()
    {
        while (_hullDamageActive)
        {
            yield return StartCoroutine(AlertFlickerRoutine(Color.red, flickerDuration));
        }
    }

    private IEnumerator AlertFlickerRoutine(Color color, float duration)
    {
        SetAlertLightsColor(color);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float sin = (Mathf.Sin(elapsed * flickerSpeed * Mathf.PI * 2f) + 1f) / 2f;
            float noise = Mathf.PerlinNoise(elapsed * flickerSpeed, 0f);
            float t = Mathf.Lerp(sin, noise, 0.5f);

            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null)
                {
                    lights[i].intensity = Mathf.Lerp(
                        _originalIntensities[i] * flickerMinIntensity,
                        _originalIntensities[i] * flickerMaxIntensity, t);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
                lights[i].intensity = _originalIntensities[i];
        }
    }

    private void RestoreLights()
    {
        if (_originalIntensities != null)
        {
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null)
                    lights[i].intensity = _originalIntensities[i];
            }
        }

        if (_originalSaved) SetAlertLightsColor(_originalColor);
        _originalSaved = false;
    }

    private void SetAlertLightsColor(Color color)
    {
        foreach (var light in lights)
        {
            if (light != null)
                light.color = color;
        }
    }

    #endregion

    #region Fading Logic (From FadeManager)

    public Coroutine FadeLight(Light light, float targetIntensity, float duration)
    {
        return StartCoroutine(FadeLightRoutine(light, targetIntensity, duration));
    }

    public Coroutine FadeMaterialProperty(Renderer renderer, string propertyName, float targetValue, float duration)
    {
        return StartCoroutine(FadeMaterialRoutine(renderer, propertyName, targetValue, duration));
    }

    public Coroutine FadeUI(CanvasGroup canvasGroup, float targetAlpha, float duration)
    {
        return StartCoroutine(FadeUIRoutine(canvasGroup, targetAlpha, duration));
    }

    public void FadeLights(IEnumerable<Light> lights, float targetIntensity, float duration)
    {
        foreach (var light in lights)
            FadeLight(light, targetIntensity, duration);
    }

    private IEnumerator FadeLightRoutine(Light light, float target, float duration)
    {
        if (light == null) yield break;
        float startIntensity = light.intensity;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            light.intensity = Mathf.Lerp(startIntensity, target, elapsed / duration);
            yield return null;
        }
        light.intensity = target;
    }

    private IEnumerator FadeMaterialRoutine(Renderer renderer, string propertyName, float target, float duration)
    {
        if (renderer == null) yield break;
        Material mat = renderer.material;
        if (!mat.HasProperty(propertyName))
        {
            Debug.LogWarning($"LightManager: El material en {renderer.name} no tiene la propiedad {propertyName}");
            yield break;
        }
        float startValue = mat.GetFloat(propertyName);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            mat.SetFloat(propertyName, Mathf.Lerp(startValue, target, elapsed / duration));
            yield return null;
        }
        mat.SetFloat(propertyName, target);
    }

    private IEnumerator FadeUIRoutine(CanvasGroup group, float target, float duration)
    {
        if (group == null) yield break;
        float startAlpha = group.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(startAlpha, target, elapsed / duration);
            yield return null;
        }
        group.alpha = target;
    }

    #endregion
}
