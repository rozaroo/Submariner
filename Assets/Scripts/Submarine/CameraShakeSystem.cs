using System.Collections;
using UnityEngine;

// Sacude la cámara cada vez que aparece una grieta nueva.
// Se puede activar y desactivar desde el Inspector sin tocar código.
public class CameraShakeSystem : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CrackManager crackManager;
    [SerializeField] private Camera targetCamera;

    [Header("Configuración")]
    [SerializeField] private bool shakeEnabled = true;
    [SerializeField] private float shakeDuration  = 0.3f;
    [SerializeField] private float shakeMagnitude = 0.15f;

    private void OnEnable()
    {
        crackManager.OnCrackSpawned += TriggerShake;
    }

    private void OnDisable()
    {
        crackManager.OnCrackSpawned -= TriggerShake;
    }

    private void TriggerShake()
    {
        if (!shakeEnabled) return;
        StopAllCoroutines();
        StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        Vector3 originalLocalPos = targetCamera.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            // La intensidad disminuye a medida que pasa el tiempo
            float strength = shakeMagnitude * (1f - elapsed / shakeDuration);
            targetCamera.transform.localPosition = originalLocalPos + Random.insideUnitSphere * strength;
            elapsed += Time.deltaTime;
            yield return null;
        }

        targetCamera.transform.localPosition = originalLocalPos;
    }
}
