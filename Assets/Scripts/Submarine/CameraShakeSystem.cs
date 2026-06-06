using System.Collections;
using UnityEngine;

public class CameraShakeSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;

    [Header("Settings")]
    [SerializeField] private bool shakeEnabled = true;
    [SerializeField] private float shakeDuration  = 0.3f;
    [SerializeField] private float shakeMagnitude = 0.15f;
    
    private Coroutine _shakeCoroutine;
    private float _lastActiveCrackCount;

    private void OnEnable()
    {
        GameEventChannel<OnHullPropertyChange>.OnEventRaised += TriggerShake;
    }

    private void OnDisable()
    {
        GameEventChannel<OnHullPropertyChange>.OnEventRaised -= TriggerShake;
    }

    private void TriggerShake(OnHullPropertyChange onHullPropertyChange)
    {
        if (!shakeEnabled) return;
        if (onHullPropertyChange.activeHullDamage <= _lastActiveCrackCount)
        {
            _lastActiveCrackCount = onHullPropertyChange.activeHullDamage;
            return;
        }
        _lastActiveCrackCount = onHullPropertyChange.activeHullDamage;
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(ShakeRoutine());
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
