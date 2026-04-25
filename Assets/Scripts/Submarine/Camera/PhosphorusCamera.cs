using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PhosphorusCamera : MonoBehaviour 
{
    [Header("Components")]
    [SerializeField] private Camera exteriorCamera;

    [Header("Settings")] 
    [SerializeField] private CameraPropertyData cameraPropertyData;
    
    [Header("Events")]
    [SerializeField] private CameraPropertiesEventChannelSO onPeriscopePhotoTaken;
    [SerializeField] private EnergyStatusEventSO energyStatusEventSo;
    
    private EnergyStatus _energyStatus = EnergyStatus.Full;
    private bool _isProcessingPhoto = false;

    #region StartUpLogic

    private void OnEnable() 
    {
        if (energyStatusEventSo != null) energyStatusEventSo.OnEventRaised += UpdateEnergyStatus;
    }

    private void OnDisable()
    {
        if (energyStatusEventSo != null) energyStatusEventSo.OnEventRaised -= UpdateEnergyStatus;
    }

    private void UpdateEnergyStatus(EnergyStatus newStatus)
    {
        _energyStatus = newStatus;
    }
    #endregion

    #region PhotoLogic

    public void TryTakePhoto()
    {
        Debug.Log("Trying to take photo - " + _energyStatus + " - " + _isProcessingPhoto);
        if (_energyStatus == EnergyStatus.Empty || _isProcessingPhoto) return;
        StartCoroutine(TakePhotoCooldownRoutine());
    }
    
    private IEnumerator TakePhotoCooldownRoutine()
    {
        _isProcessingPhoto = true;
        
        exteriorCamera.enabled = true;
        exteriorCamera.Render(); 
        exteriorCamera.enabled = false;
        
        if (onPeriscopePhotoTaken != null)
        {
            onPeriscopePhotoTaken.RaiseEvent(cameraPropertyData);
        }
        float totalCooldownTime = cameraPropertyData._VisibleDuration + cameraPropertyData._fadeDuration;
        
        yield return new WaitForSeconds(totalCooldownTime);
        
        _isProcessingPhoto = false;
    }

    #endregion
}