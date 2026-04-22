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
    [SerializeField] private EnergyStatusEventSO _energyStatusEventSO;
    
    private EnergyStatus _energyStatus;
    private bool _isProcessingPhoto = false;
    
    private void OnEnable()
    {
        if (_energyStatusEventSO != null) _energyStatusEventSO.OnEventRaised += UpdateEnergyStatus;
    }

    private void OnDisable()
    {
        if (_energyStatusEventSO != null) _energyStatusEventSO.OnEventRaised -= UpdateEnergyStatus;
    }
    
    private void UpdateEnergyStatus(EnergyStatus newStatus)
    {
        _energyStatus = newStatus;
    }
    
    public void TryTakePhoto()
    {
        if (_energyStatus != EnergyStatus.Empty || _isProcessingPhoto) return;
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