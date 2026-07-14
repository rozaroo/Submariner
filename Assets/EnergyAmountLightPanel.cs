using System.Collections.Generic;
using UnityEngine;

public class EnergyAmountLightPanel : MonoBehaviour
{
    [Header("Stress Parameters")]
    [Tooltip("Number of stress lights, needs to be in order")]
    [SerializeField] private List<GameObject> energyLightsGo = new List<GameObject>();
    [SerializeField] private Color onColor = Color.green;
    [SerializeField] private float onColorIntensity = 0.1f;
    [SerializeField] private Color offColor = Color.red;
    [SerializeField] private float offColorIntensity = 0.1f;

    [SerializeField] private List<LightObject> _lightObjects = new List<LightObject>();
    private float _previousPercentage = -1f;

    private void Start()
    {
        foreach (GameObject go in energyLightsGo)
        {
            if (go.TryGetComponent(out LightObject lightObject))
            {
                _lightObjects.Add(lightObject);
            }
        }
        ApplyLightStartup();
    }

    private void ApplyLightStartup()
    {
        foreach (LightObject lightObject in _lightObjects)
        {
            if (lightObject != null)
            {
                lightObject.Initialize();
            }
        }
        UpdateLights(100f, 100f);
    }

    private void OnEnable()
    {
        GameEventChannel<OnEnergyPropertyChange>.OnEventRaised += OnEnergyChange;
    }

    private void OnDisable()
    {
        GameEventChannel<OnEnergyPropertyChange>.OnEventRaised -= OnEnergyChange;
    }

    private void OnEnergyChange(OnEnergyPropertyChange propertyChange)
    {
        UpdateLights(propertyChange.currentEnergyPercentage, propertyChange.maxEnergyPercentage);
    }

    private void UpdateLights(float percentage, float maxPercentage)
    {
        if (Mathf.Approximately(percentage, _previousPercentage))
        {
            return;
        }
        _previousPercentage = percentage;

        int totalLights = _lightObjects.Count;
        if (totalLights == 0) return;

        float step = maxPercentage / totalLights;

        for (int i = 0; i < totalLights; i++)
        {
            float bandBottom = i * step;
            float bandTop = bandBottom + step;

            float t;

            if (percentage >= bandTop)
            {
                t = 1f;
            }
            else if (percentage <= bandBottom)
            {
                t = 0f;
            }
            else
            {
                t = (percentage - bandBottom) / step;
            }

            ApplyLightState(i, t);
        }
    }

    private void ApplyLightState(int index, float t)
    {
        LightObject light = _lightObjects[index];
        light.SetColor(Color.Lerp(offColor, onColor, t));
        light.SetIntensity(Mathf.Lerp(offColorIntensity, onColorIntensity, t));
    }
}