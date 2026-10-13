using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LightManager : MonoBehaviour
{
    public static LightManager Instance { get; private set; }

    [Header("Alert Config")]
    [SerializeField] private LightObject[] lightsObjects;
    [SerializeField] private float flickerSpeed = 2f;
    [SerializeField] private float flickerMinIntensity = 0.2f;
    [SerializeField] private float flickerMaxIntensity = 1.5f;
    [SerializeField] private float blackoutIntensity = 0.05f;
    [SerializeField] private float flickerDuration = 3f;
    [SerializeField] private float alertCycleDuration = 1f;
    [SerializeField] private float restoreFadeDuration = 2f; // Tiempo que tarda la luz en volver lentamente

    [Header("Light Themes")]
    [SerializeField] private Color hullDamageColor = Color.red;
    [SerializeField] private Color lowOxygenColor = new Color(0f, 0.5f, 1f);

    private Color[] _originalColors;
    private float[] _originalIntensities;
    private bool _originalSaved;

    private bool _hullDamageActive;
    private bool _lowOxygenActive;
    private bool _isBlackout;

    private Coroutine _alertCoroutine;
    private Coroutine _restoreFadeCoroutine;

    public bool IsAlertActive => _hullDamageActive || _lowOxygenActive;
    public bool IsBlackout => _isBlackout;
    public bool IsRestoring => _restoreFadeCoroutine != null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        foreach (LightObject lights in lightsObjects)
        {
            if (lights != null)
                lights.Initialize();
        }

        // Guardamos el color y la intensidad original apenas se inicializan las luces
        SaveOriginalState();
    }

    private void OnEnable()
    {
        GameEventChannel<OnHullPropertyChange>.OnEventRaised += OnHullStatusChanged;
        GameEventChannel<OnLowOxygen>.OnEventRaised += OnLowOxygenStatusChanged;
        GameEventChannel<OnEnergyStatusChange>.OnEventRaised += OnEnergyStatusChanged;
    }

    private void OnDisable()
    {
        GameEventChannel<OnHullPropertyChange>.OnEventRaised -= OnHullStatusChanged;
        GameEventChannel<OnLowOxygen>.OnEventRaised -= OnLowOxygenStatusChanged;
        GameEventChannel<OnEnergyStatusChange>.OnEventRaised -= OnEnergyStatusChanged;
    }

    #region Alert Logic

    private void OnEnergyStatusChanged(OnEnergyStatusChange status)
    {
        bool wasBlackout = _isBlackout;
        _isBlackout = (status.energyStatus == EnergyStatus.Empty);

        // Solo actualizamos el estado de luces si realmente entramos o salimos de Blackout
        if (wasBlackout != _isBlackout)
        {
            UpdateAlertState();
        }
    }

    private void OnHullStatusChanged(OnHullPropertyChange onHullPropertyChange)
    {
        bool hasDamage = onHullPropertyChange.activeHullDamage > 0;
        if (_hullDamageActive == hasDamage) return;

        _hullDamageActive = hasDamage;
        UpdateAlertState();
    }

    private void OnLowOxygenStatusChanged(OnLowOxygen onLowOxygen)
    {
        if (_lowOxygenActive == onLowOxygen.IsLow) return;

        _lowOxygenActive = onLowOxygen.IsLow;
        UpdateAlertState();
    }

    private void SaveOriginalState()
    {
        if (_originalSaved || lightsObjects == null || lightsObjects.Length == 0) return;

        _originalColors = new Color[lightsObjects.Length];
        _originalIntensities = new float[lightsObjects.Length];

        for (int i = 0; i < lightsObjects.Length; i++)
        {
            if (lightsObjects[i] != null)
            {
                _originalColors[i] = lightsObjects[i].LightColor;
                _originalIntensities[i] = lightsObjects[i].Intensity > blackoutIntensity ? lightsObjects[i].Intensity : 1.5f;
            }
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

        if (_restoreFadeCoroutine != null)
        {
            StopCoroutine(_restoreFadeCoroutine);
            _restoreFadeCoroutine = null;
        }

        // Prioridad 1: Apagón total (Fusible quemado / Sin energía)
        if (_isBlackout)
        {
            RestoreOriginalColors();

            for (int i = 0; i < lightsObjects.Length; i++)
            {
                if (lightsObjects[i] != null)
                {
                    lightsObjects[i].Toggle(true);
                    lightsObjects[i].SetIntensity(blackoutIntensity);
                }
            }
            return;
        }

        // Prioridad 2: Si NO hay daño en el casco Y el oxígeno está bien -> vuelve la luz lentamente
        if (!_hullDamageActive && !_lowOxygenActive)
        {
            _restoreFadeCoroutine = StartCoroutine(RestoreLightsSlowlyRoutine());
            return;
        }

        // Prioridad 3: Hay al menos una alerta activa (HullDamage, LowOxygen, o ambas)
        for (int i = 0; i < lightsObjects.Length; i++)
        {
            if (lightsObjects[i] != null)
                lightsObjects[i].Toggle(true);
        }

        _alertCoroutine = StartCoroutine(AlertRoutine());
    }

    private IEnumerator AlertRoutine()
    {
        bool alternateColor = false;

        while (!_isBlackout && (_hullDamageActive || _lowOxygenActive))
        {
            Color currentColorAlert;

            if (_hullDamageActive && _lowOxygenActive)
            {
                currentColorAlert = alternateColor ? hullDamageColor : lowOxygenColor;
                alternateColor = !alternateColor;
            }
            else
            {
                currentColorAlert = _hullDamageActive ? hullDamageColor : lowOxygenColor;
                alternateColor = false;
            }

            yield return StartCoroutine(AlertPulseRoutine(currentColorAlert, alertCycleDuration));
        }
    }

    private IEnumerator AlertPulseRoutine(Color color, float duration)
    {
        SetAlertLightsColor(color);
        float elapsed = 0f;

        while (elapsed < duration && !_isBlackout && (_hullDamageActive || _lowOxygenActive))
        {
            float t = Mathf.Sin((elapsed / duration) * Mathf.PI);

            for (int i = 0; i < lightsObjects.Length; i++)
            {
                if (lightsObjects[i] != null)
                    lightsObjects[i].SetIntensity(Mathf.Lerp(flickerMinIntensity, flickerMaxIntensity, t));
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator RestoreLightsSlowlyRoutine()
    {
        SaveOriginalState();

        float[] startIntensities = new float[lightsObjects.Length];
        Color[] startColors = new Color[lightsObjects.Length];

        for (int i = 0; i < lightsObjects.Length; i++)
        {
            if (lightsObjects[i] != null)
            {
                lightsObjects[i].Toggle(true);
                startIntensities[i] = lightsObjects[i].Intensity;
                startColors[i] = lightsObjects[i].LightColor;
            }
        }

        float elapsed = 0f;
        while (elapsed < restoreFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / restoreFadeDuration);

            for (int i = 0; i < lightsObjects.Length; i++)
            {
                if (lightsObjects[i] != null)
                {
                    lightsObjects[i].SetColor(Color.Lerp(startColors[i], _originalColors[i], t));
                    lightsObjects[i].SetIntensity(Mathf.Lerp(startIntensities[i], _originalIntensities[i], t));
                }
            }
            yield return null;
        }

        RestoreOriginalColors();
        for (int i = 0; i < lightsObjects.Length; i++)
        {
            if (lightsObjects[i] != null)
            {
                lightsObjects[i].Toggle(true);
                lightsObjects[i].SetIntensity(_originalIntensities[i]);
            }
        }

        _restoreFadeCoroutine = null;
    }

    private void RestoreOriginalColors()
    {
        if (!_originalSaved || _originalColors == null) return;

        for (int i = 0; i < lightsObjects.Length; i++)
        {
            if (lightsObjects[i] != null)
                lightsObjects[i].SetColor(_originalColors[i]);
        }
    }

    private void SetAlertLightsColor(Color color)
    {
        foreach (var light in lightsObjects)
        {
            if (light != null)
                light.SetColor(color);
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