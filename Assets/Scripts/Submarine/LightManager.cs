using UnityEngine;
using System.Collections;

public class LightManager : MonoBehaviour
{
    [Header("Luces")]
    [SerializeField] private Light[] lights;
    [SerializeField] private float flickerSpeed = 2f;
    [SerializeField] private float flickerMinIntensity = 0.2f;
    [SerializeField] private float flickerMaxIntensity = 1.5f;
    [SerializeField] private float flickerDuration = 3f;
    // Duración de cada color durante la intercalación rojo/azul
    [SerializeField] private float alertCycleDuration = 1f;

    [Header("Event Channels")]
    [SerializeField] private HullPropertyEventSO onHullStatusChanged;
    [SerializeField] private OxygenSystem oxygenSystem;

    private Color _originalColor;
    private float[] _originalIntensities;
    private bool _originalSaved;

    private bool _hullDamageActive;
    private bool _lowOxygenActive;
    private float _lastActiveCrackCount;

    private Coroutine _alertCoroutine;

    private void OnEnable()
    {
        onHullStatusChanged.OnEventRaised += OnHullStatusChanged;
        if (oxygenSystem != null) oxygenSystem.OnLowOxygen += OnLowOxygen;
    }

    private void OnDisable()
    {
        onHullStatusChanged.OnEventRaised -= OnHullStatusChanged;
        if (oxygenSystem != null) oxygenSystem.OnLowOxygen -= OnLowOxygen;
    }

    private void OnHullStatusChanged(HullProperty hullProperty)
    {
        bool increased = hullProperty.activeHullDamage > _lastActiveCrackCount;
        _lastActiveCrackCount = hullProperty.activeHullDamage;
        _hullDamageActive = hullProperty.activeHullDamage > 0;

        if (increased) SaveOriginalIfNeeded();
        UpdateAlertState();
    }

    private void OnLowOxygen(bool isLow)
    {
        _lowOxygenActive = isLow;
        if (isLow) SaveOriginalIfNeeded();
        UpdateAlertState();
    }

    private void SaveOriginalIfNeeded()
    {
        if (_originalSaved) return;
        _originalColor = lights[0].color;
        _originalIntensities = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++)
            _originalIntensities[i] = lights[i].intensity;
        _originalSaved = true;
    }

    private void UpdateAlertState()
    {
        if (_alertCoroutine != null)
        {
            StopCoroutine(_alertCoroutine);
            _alertCoroutine = null;
        }

        if (!_hullDamageActive && !_lowOxygenActive)
        {
            RestoreLights();
            return;
        }

        _alertCoroutine = StartCoroutine(AlertRoutine());
    }

    private IEnumerator AlertRoutine()
    {
        while (_hullDamageActive || _lowOxygenActive)
        {
            if (_hullDamageActive)
            {
                // Usa alertCycleDuration para la intercalación, flickerDuration solo al primer impacto
                bool alternating = _lowOxygenActive;
                yield return StartCoroutine(FlickerRoutine(Color.red, alternating ? alertCycleDuration : flickerDuration));
                if (!_lowOxygenActive) SetLightColor(Color.red);
            }

            if (_lowOxygenActive)
            {
                bool alternating = _hullDamageActive;
                yield return StartCoroutine(FlickerRoutine(Color.blue, alternating ? alertCycleDuration : flickerDuration));
                if (!_hullDamageActive) SetLightColor(Color.blue);
            }
        }
    }

    private IEnumerator FlickerRoutine(Color color, float duration)
    {
        SetLightColor(color);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float sin   = (Mathf.Sin(elapsed * flickerSpeed * Mathf.PI * 2f) + 1f) / 2f;
            float noise = Mathf.PerlinNoise(elapsed * flickerSpeed, 0f);
            float t     = Mathf.Lerp(sin, noise, 0.5f);

            for (int i = 0; i < lights.Length; i++)
                lights[i].intensity = Mathf.Lerp(
                    _originalIntensities[i] * flickerMinIntensity,
                    _originalIntensities[i] * flickerMaxIntensity, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < lights.Length; i++)
            lights[i].intensity = _originalIntensities[i];
    }

    private void RestoreLights()
    {
        if (_originalIntensities != null)
            for (int i = 0; i < lights.Length; i++)
                lights[i].intensity = _originalIntensities[i];

        if (_originalSaved) SetLightColor(_originalColor);
        _originalSaved = false;
    }

    private void SetLightColor(Color color)
    {
        foreach (var light in lights)
            light.color = color;
    }
}
