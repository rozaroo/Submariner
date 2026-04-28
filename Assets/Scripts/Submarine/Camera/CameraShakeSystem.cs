using System.Collections;
using UnityEngine;

public class CameraShakeSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HullPropertyEventSO onHullStatusChanged;
    [SerializeField] private Camera targetCamera;

    [Header("Settings")]
    [SerializeField] private bool shakeEnabled = true;
    [SerializeField] private float shakeDuration  = 0.3f;
    [SerializeField] private float shakeMagnitude = 0.15f;
    
    private Coroutine _shakeCoroutine;

    private void OnEnable()
    {
        onHullStatusChanged.OnEventRaised += TriggerShake;
    }

    private void OnDisable()
    {
        onHullStatusChanged.OnEventRaised -= TriggerShake;
    }

    private void TriggerShake(HullProperty hullProperty)
    {
        if (!shakeEnabled) return;
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
        }
        StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        Vector3 originalLocalPos = targetCamera.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float strength = shakeMagnitude * (1f - elapsed / shakeDuration);
            targetCamera.transform.localPosition = originalLocalPos + Random.insideUnitSphere * strength;
            elapsed += Time.deltaTime;
            yield return null;
        }
        targetCamera.transform.localPosition = originalLocalPos;
    }
}
