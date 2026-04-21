using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PhosphorusCamera : MonoBehaviour 
{
    [Header("Componentes")]
    [SerializeField] private Camera _exteriorCamera;

    [Header("Settings")] 
    [SerializeField] private CameraPropertyData _cameraPropertyData;
    
    [Header("Events")]
    [SerializeField] private CameraPropertiesEventChannelSO _onPeriscopePhotoTaken;

    private bool _hasEnergy = true;
    private bool _isProcessingPhoto = false;
    public void TryTakePhoto()
    {
        if (!_hasEnergy || _isProcessingPhoto) return;
        StartCoroutine(TakePhotoCooldownRoutine());
    }
    
    private IEnumerator TakePhotoCooldownRoutine()
    {
        _isProcessingPhoto = true;
        
        _exteriorCamera.enabled = true;
        _exteriorCamera.Render(); 
        _exteriorCamera.enabled = false;
        
        if (_onPeriscopePhotoTaken != null)
        {
            _onPeriscopePhotoTaken.RaiseEvent(_cameraPropertyData);
        }
        float totalCooldownTime = _cameraPropertyData._VisibleDuration + _cameraPropertyData._fadeDuration;
        
        yield return new WaitForSeconds(totalCooldownTime);
        
        _isProcessingPhoto = false;
    }
}