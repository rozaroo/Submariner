using System.Collections;
using UnityEngine;

public class GalvanometerIndicator : MonoBehaviour
{
    [Header("Indicator Settings")]
    [SerializeField] private float minRotation = -45f;
    [SerializeField] private float maxRotation = 45f;
    
    [Header("Tremble Settings")]
    [SerializeField, Range(0f, 1f)] private float trembleThreshold = 0.9f; 
    [SerializeField] private float trembleIntensity = 5f;
    [SerializeField] private float trembleSpeed = 30f;

    [Header("Indicator Components")]
    [SerializeField] private Transform needleTransform;

    private Coroutine _trembleCoroutine;
    private float _trembleWeight;
    private float _currentRotationDataX;
    
    public void UpdateIndicator(float currentValue, float originalMin, float originalMax)
    {
        float normalizedValue = Mathf.InverseLerp(originalMin, originalMax, currentValue);
        _currentRotationDataX = Mathf.Lerp(minRotation, maxRotation, normalizedValue);
        
        float edgeProximity = Mathf.Abs(normalizedValue - 0.5f) * 2f;
        _trembleWeight = Mathf.InverseLerp(trembleThreshold, 1f, edgeProximity);
        
        if (_trembleWeight > 0f)
        {
            StartTremble();
        }
        else
        {
            StopTremble();
        }
        
        needleTransform.localEulerAngles = new Vector3(_currentRotationDataX, 0f,0f);
    }

    private void StartTremble()
    {
        _trembleCoroutine = StartCoroutine(TrembleNeedle());
    }

    private void StopTremble()
    {
        StopCoroutine(_trembleCoroutine);
    }

    private IEnumerator TrembleNeedle()
    {
        float noise = (Mathf.PerlinNoise(Time.time * trembleSpeed, 0f) - 0.5f) * 2f;
        _currentRotationDataX += noise * trembleIntensity * _trembleWeight;
        yield return null;
    }
}