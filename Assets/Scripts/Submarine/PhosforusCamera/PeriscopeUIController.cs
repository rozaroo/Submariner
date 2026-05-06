using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PeriscopeUIController : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private RawImage _phosphorusDisplay;
    [SerializeField] private GameObject _periscopeContainer;
    
    [Header("Flash Settings")]
    [SerializeField] private Image _flashOverlay;
    [SerializeField] private float _flashDuration = 0.15f;

    [Header("Events Channels")]
    [SerializeField] private BaseEventChannelSO _onPeriscopePossess;
    [SerializeField] private BaseEventChannelSO _onPeriscopeUnpossess;
    [SerializeField] private CameraPropertiesEventChannelSO _onPeriscopePhotoTaken;

    private Coroutine _photoRoutine;

    private void OnEnable()
    {
        if (_onPeriscopePossess != null) _onPeriscopePossess.OnEventRaised += TurnOnScreen;
        if (_onPeriscopeUnpossess != null) _onPeriscopeUnpossess.OnEventRaised += TurnOffScreen;
        if (_onPeriscopePhotoTaken != null) _onPeriscopePhotoTaken.OnEventRaised += RenderPhoto;
    }

    private void OnDisable()
    {
        if (_onPeriscopePossess != null) _onPeriscopePossess.OnEventRaised -= TurnOnScreen;
        if (_onPeriscopeUnpossess != null) _onPeriscopeUnpossess.OnEventRaised -= TurnOffScreen;
        if (_onPeriscopePhotoTaken != null) _onPeriscopePhotoTaken.OnEventRaised -= RenderPhoto;
    }

    private void TurnOnScreen()
    {
        _periscopeContainer.SetActive(true);
        SetAlpha(0f, _phosphorusDisplay);
        SetAlpha(0f, _flashOverlay);
    }

    private void TurnOffScreen()
    {
        _periscopeContainer.SetActive(false);
        if (_photoRoutine != null) StopCoroutine(_photoRoutine);
    }

    private void RenderPhoto(CameraPropertyData properties)
    {
        if (_photoRoutine != null) StopCoroutine(_photoRoutine);
        _photoRoutine = StartCoroutine(RenderPhotoRoutine(properties));
    }

    private IEnumerator RenderPhotoRoutine(CameraPropertyData properties)
    {
        SetAlpha(1f, _phosphorusDisplay);
        
        if (_flashOverlay != null)
        {
            float flashTimer = 0f;
            while (flashTimer < _flashDuration)
            {
                flashTimer += Time.deltaTime;
                float flashAlpha = Mathf.Lerp(1f, 0f, flashTimer / _flashDuration);
                SetAlpha(flashAlpha, _flashOverlay);
                yield return null;
            }
            SetAlpha(0f, _flashOverlay);
        }
        float remainingVisibleTime = Mathf.Max(0f, properties._VisibleDuration - _flashDuration);
        yield return new WaitForSeconds(remainingVisibleTime);
        
        float elapsedTime = 0f;
        while (elapsedTime < properties._fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(1f, 0f, elapsedTime / properties._fadeDuration);
            SetAlpha(newAlpha, _phosphorusDisplay);
            yield return null;
        }
        
        SetAlpha(0f, _phosphorusDisplay);
    }
    private void SetAlpha(float alpha, Graphic graphic)
    {
        if (graphic != null)
        {
            Color c = graphic.color;
            c.a = alpha;
            graphic.color = c;
        }
    }
}