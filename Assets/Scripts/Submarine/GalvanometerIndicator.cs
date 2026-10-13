using UnityEngine;

public class GalvanometerIndicator : MonoBehaviour
{
    [Header("Indicator Settings")]
    [SerializeField] private float minRotation = -45f;
    [SerializeField] private float maxRotation = 45f;
    [SerializeField] private float smoothSpeed = 10f;

    [Header("Tremble Settings")]
    [SerializeField, Range(0f, 1f)] private float trembleThreshold = 0.9f;
    [SerializeField] private float trembleIntensity = 5f;
    [SerializeField] private float trembleSpeed = 30f;

    [Header("Indicator Components")]
    [SerializeField] private Transform needleTransform;

    private float _trembleWeight;
    private float _targetRotationX;
    private float _currentRotationX;

    private void Awake()
    {
        _targetRotationX = minRotation;
        _currentRotationX = minRotation;
    }

    public void UpdateIndicator(float currentValue, float originalMin, float originalMax)
    {
        if (Mathf.Approximately(originalMin, originalMax))
        {
            ResetIndicator();
            return;
        }

        float normalizedValue = Mathf.InverseLerp(originalMin, originalMax, currentValue);
        _targetRotationX = Mathf.Lerp(minRotation, maxRotation, normalizedValue);
        _trembleWeight = Mathf.InverseLerp(trembleThreshold, 1f, normalizedValue);
    }

    private void Update()
    {
        if (needleTransform == null) return;

        _currentRotationX = Mathf.Lerp(_currentRotationX, _targetRotationX, Time.deltaTime * smoothSpeed);
        float finalRotationX = _currentRotationX;

        if (_trembleWeight > 0f)
        {
            float noise = (Mathf.PerlinNoise((Time.time + GetInstanceID()) * trembleSpeed, 0f) - 0.5f) * 2f;
            finalRotationX += noise * trembleIntensity * _trembleWeight;
        }

        needleTransform.localEulerAngles = new Vector3(finalRotationX, 0f, 0f);
    }

    public void ResetIndicator()
    {
        _targetRotationX = minRotation;
        _trembleWeight = 0f;
    }
}