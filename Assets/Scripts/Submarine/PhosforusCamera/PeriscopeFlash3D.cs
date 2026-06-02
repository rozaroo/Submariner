using System.Collections;
using UnityEngine;

public class PeriscopeFlash3D : MonoBehaviour
{
    [SerializeField] private MeshRenderer flashRenderer;
    [SerializeField] private float flashDuration = 0.15f;

    private Material _material;
    private Coroutine _flashRoutine;

    private void Awake()
    {
        _material = flashRenderer.material;
        Color color = _material.color;
        color.a = 0f;
        _material.color = color;
        flashRenderer.enabled = false;
    }

    public void PlayFlash()
    {
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        SetAlpha(1f);
        float timer = 0f;
        while (timer < flashDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / flashDuration);
            SetAlpha(alpha);
            yield return null;
        }
        SetAlpha(0f);
    }

    private void SetAlpha(float alpha)
    {
        if (alpha <= 0.01f)
        {
            flashRenderer.enabled = false;
            return;
        }
        flashRenderer.enabled = true;
        Color color = _material.color;
        color.a = alpha;
        _material.color = color;
    }
}
