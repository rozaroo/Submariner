using System;
using UnityEngine;

public class StationSwitchLightController : MonoBehaviour
{
    [Header("Station Lights")]
    [SerializeField] private LightObject[] stationLights;

    [Header("Light Settings")]
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private float activeColorIntensity = 0.1f;
    [SerializeField] private Color deactivatedColor = Color.red;
    [SerializeField] private float deactivatedColorIntensity = 0.1f;

    [Header("Warning Light Settings")]
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private float warningColorIntensity = 0.1f;
    [SerializeField, Tooltip("Referencia al motor. Si se asigna y el motor no está encendido al intentar activar, la luz será amarilla.")]
    private EngineSystem engineSystem;

    private IActivatable _activationElement;

    private void OnEnable()
    {
        if (TryGetComponent(out _activationElement))
        {
            _activationElement.onActivation += ActivateLights;
            _activationElement.onDeactivation += DeactivateLights;

            foreach (var lights in stationLights)
            {
                lights.Initialize();
            }

            Log.Info(_activationElement.GetType().Name + " activated");

            if (_activationElement.isActive)
            {
                ActivateLights();
            }
            else
            {
                DeactivateLights();
            }
        }

        if (engineSystem != null)
        {
            engineSystem.OnEngineStartedEvent += CheckLightState;
            engineSystem.OnEngineStoppedEvent += CheckLightState; // Escucha la detención
        }
    }

    private void OnDisable()
    {
        if (_activationElement != null)
        {
            _activationElement.onActivation -= ActivateLights;
            _activationElement.onDeactivation -= DeactivateLights;
        }

        if (engineSystem != null)
        {
            engineSystem.OnEngineStartedEvent -= CheckLightState;
            engineSystem.OnEngineStoppedEvent -= CheckLightState;
        }
    }

    private void CheckLightState()
    {
        if (_activationElement != null && _activationElement.isActive)
        {
            ActivateLights();
        }
    }

    private void ActivateLights()
    {
        // Verifica si existe la referencia al motor y si el motor NO está en marcha
        if (engineSystem != null && !engineSystem.IsRunning())
        {
            foreach (var lights in stationLights)
            {
                lights.SetColor(warningColor);
                lights.SetIntensity(warningColorIntensity);
            }
            return;
        }

        foreach (var lights in stationLights)
        {
            lights.SetColor(activeColor);
            lights.SetIntensity(activeColorIntensity);
        }
    }

    private void DeactivateLights()
    {
        foreach (var lights in stationLights)
        {
            lights.SetColor(deactivatedColor);
            lights.SetIntensity(deactivatedColorIntensity);
        }
    }

    private void TurnOnLights()
    {
        foreach (var lights in stationLights)
        {
            lights.Toggle(true);
        }
    }

    private void TurnOffLights()
    {
        foreach (var lights in stationLights)
        {
            lights.Toggle(false);
        }
    }
}