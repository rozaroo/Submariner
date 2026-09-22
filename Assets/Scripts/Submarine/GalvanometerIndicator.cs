using UnityEngine;

public class GalvanometerIndicator : MonoBehaviour
{
    [Header("Indicator Settings")]
    [SerializeField] private float minValue = 0f;
    [SerializeField] private float maxValue = 100f;
    [SerializeField] private float minRotation = -90f;
    [SerializeField] private float maxRotation = 90f;

    [Header("Indicator Components")]
    [SerializeField] private Transform needleTransform;

    public void UpdateIndicator(float value)
    {
        value = Mathf.Clamp(value, minValue, maxValue);
        float normalizedValue = (value - minValue) / (maxValue - minValue);
        float rotationZ = Mathf.Lerp(minRotation, maxRotation, normalizedValue);
        needleTransform.localEulerAngles = new Vector3(0f, 0f, rotationZ);
    }
}
