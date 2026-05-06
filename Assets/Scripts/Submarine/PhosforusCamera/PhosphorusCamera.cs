using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PhosphorusCamera : MonoBehaviour 
{
    [Header("References")]
    [SerializeField] private Camera exteriorCamera;

    [Header("Camera Settings")] 
    [SerializeField] private CameraPropertyData cameraPropertyData;
    
    [Header("Event Channels")]
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
        if (_energyStatus == EnergyStatus.Empty)
        {
            if (_isProcessingPhoto)
            {
                StopAllCoroutines();
                _isProcessingPhoto = false;
                exteriorCamera.enabled = false;
                Log.Info("Canceled Photo - No Energy");
            }
        }
    }
    #endregion

    #region PhotoLogic

    public void TryTakePhoto()
    {
        Log.Info("Trying to take photo - " + _energyStatus + " - " + _isProcessingPhoto);
        if (_energyStatus == EnergyStatus.Empty)
        {
            Log.Info("No Energy = No Photo"); //TODO: Agregar sonidos en caso de que no haya Energia para sacar la foto.
            return;
        }
        if (_isProcessingPhoto) return;
        StartCoroutine(TakePhotoCooldownRoutine());
    }

    private IEnumerator TakePhotoCooldownRoutine()
    {
        _isProcessingPhoto = true;
        // chequeo extra por seguridad
        if (_energyStatus == EnergyStatus.Empty)
        {
            Log.Info("No Energy = No Photo");
            _isProcessingPhoto = false;
            yield break;
        }
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