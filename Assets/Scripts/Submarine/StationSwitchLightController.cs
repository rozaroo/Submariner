using System;
using UnityEngine;

public class StationSwitchLightController : MonoBehaviour
{
    [Header("Station Lights")]
    [SerializeField] private LightObject[] stationLights; 
    
    [Header("Light Settings")]
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private float activeColorIntensity = 0.01f;
    [SerializeField] private Color deactivatedColor = Color.red;
    [SerializeField] private float deactivatedColorIntensity = 0.01f;
    
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
    }
    
    private void OnDisable()
    {
        if (_activationElement != null)
        {
            _activationElement.onActivation -= ActivateLights;
            _activationElement.onDeactivation -= DeactivateLights;
        }
    }

    private void ActivateLights()
    {
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
