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
    private Color originalColor;
    private float[] _originalIntensities;
    
    private void OnEnable()
    {
        onHullStatusChanged.OnEventRaised += FlickerLights;
    }
    
    private void OnDisable()
    {
        onHullStatusChanged.OnEventRaised -= FlickerLights;
    }

    private void ChangeLightColor(Color color)
    {
        foreach (var light in lights)
        {
            originalColor = light.color;
            light.color = color;
        }
    }
    
    private void FlickerLights(HullProperty hullProperty)
    {
        FlickerLights();
    }
    
    private void FlickerLights()
    {
        if (_flickerCoroutine != null)
        {
            StopCoroutine(_flickerCoroutine);
        }
        ChangeLightColor(Color.red);
        _flickerCoroutine = StartCoroutine(FlickerRoutine());
    }
    
    private IEnumerator FlickerRoutine()
    {
        _originalIntensities = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++)
        {
            _originalIntensities[i] = lights[i].intensity;
        }


        float elapsedTime = 0f;

        while (elapsedTime < flickerDuration)
        {
            float sin = (Mathf.Sin(elapsedTime * flickerSpeed * Mathf.PI * 2f) + 1f) / 2f;
            float noise = Mathf.PerlinNoise(elapsedTime * flickerSpeed, 0f);
            float t = Mathf.Lerp(sin, noise, 0.5f);

            foreach (var light in lights)
            {
                float originalIntensity = _originalIntensities[Array.IndexOf(lights, light)];
                light.intensity = Mathf.Lerp(originalIntensity * flickerMinIntensity, originalIntensity * flickerMaxIntensity, t);
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < lights.Length; i++)
            lights[i].intensity = _originalIntensities[i];
        ChangeLightColor(originalColor);
    }
}
