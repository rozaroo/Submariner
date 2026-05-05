using System;
using UnityEngine;
using System.Collections;

public class LightManager : MonoBehaviour
{
    [Header("Light Settings")]
    [SerializeField] private Light[] lights;
    [SerializeField] private float flickerSpeed = 2f;
    [SerializeField] private float flickerMinIntensity = 0.2f;
    [SerializeField] private float flickerMaxIntensity = 1.5f;
    [SerializeField] private float flickerDuration = 5f;

    [Header("Event Channels")]
    [SerializeField] private HullPropertyEventSO onHullStatusChanged;

    private Coroutine _flickerCoroutine;
    private Color _originalColor;
    private float[] _originalIntensities;
    private bool _isFlickering;
    private float _lastActiveCrackCount;

    private void OnEnable()  => onHullStatusChanged.OnEventRaised += OnHullStatusChanged;
    private void OnDisable() => onHullStatusChanged.OnEventRaised -= OnHullStatusChanged;

    private void OnHullStatusChanged(HullProperty hullProperty)
    {
        if (hullProperty.activeHullDamage <= 0)
        {
            RestoreLights();
            _lastActiveCrackCount = 0;
            return;
        }

        // Solo dispara el parpadeo de impacto si el daño aumentó
        if (hullProperty.activeHullDamage > _lastActiveCrackCount)
            TriggerFlicker();

        _lastActiveCrackCount = hullProperty.activeHullDamage;
    }

    private void TriggerFlicker()
    {
        if (_flickerCoroutine != null)
            StopCoroutine(_flickerCoroutine);

        if (!_isFlickering)
        {
            _originalColor = lights[0].color;
            _originalIntensities = new float[lights.Length];
            for (int i = 0; i < lights.Length; i++)
                _originalIntensities[i] = lights[i].intensity;
        }

        _isFlickering = true;
        SetLightColor(Color.red);
        _flickerCoroutine = StartCoroutine(FlickerRoutine());
    }

    private IEnumerator FlickerRoutine()
    {
        float elapsed = 0f;
        while (elapsed < flickerDuration)
        {
            float sin   = (Mathf.Sin(elapsed * flickerSpeed * Mathf.PI * 2f) + 1f) / 2f;
            float noise = Mathf.PerlinNoise(elapsed * flickerSpeed, 0f);
            float t     = Mathf.Lerp(sin, noise, 0.5f);

            for (int i = 0; i < lights.Length; i++)
                lights[i].intensity = Mathf.Lerp(_originalIntensities[i] * flickerMinIntensity,
                                                  _originalIntensities[i] * flickerMaxIntensity, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Al terminar el parpadeo las luces quedan rojas con intensidad normal hasta que se reparen todas
        for (int i = 0; i < lights.Length; i++)
            lights[i].intensity = _originalIntensities[i];
    }

    private void RestoreLights()
    {
        if (_flickerCoroutine != null)
        {
            StopCoroutine(_flickerCoroutine);
            _flickerCoroutine = null;
        }

        if (_originalIntensities != null)
            for (int i = 0; i < lights.Length; i++)
                lights[i].intensity = _originalIntensities[i];

        SetLightColor(_originalColor);
        _isFlickering = false;
    }

    private void SetLightColor(Color color)
    {
        foreach (var light in lights)
            light.color = color;
    }
}
