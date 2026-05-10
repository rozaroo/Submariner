using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class PhosphorusCamera : MonoBehaviour 
{
    [Header("References")]
    [SerializeField] private Camera exteriorCamera;

    [Header("Camera Settings")] 
    [SerializeField] private CameraPropertyData cameraPropertyData;
    
    [Header("Event Channels")]
    [SerializeField] private EnergyStatusEventSO energyStatusEventSo;
    
    private EnergyStatus _energyStatus = EnergyStatus.Full;
    private bool _isProcessingPhoto = false;

    [Header("Rotation Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalClamp = 70f;

    private float _pitch;
    private float _yaw;
    private bool _isActiveView;

    #region StartUpLogic

    private void OnEnable() 
    {
        if (energyStatusEventSo != null) energyStatusEventSo.OnEventRaised += UpdateEnergyStatus;
    }

    private void OnDisable()
    {
        if (energyStatusEventSo != null) energyStatusEventSo.OnEventRaised -= UpdateEnergyStatus;
    }
    private void Update()
    {
        if (!_isActiveView) return;
        if (Mouse.current == null) return;
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        _yaw += mouseDelta.x * mouseSensitivity * Time.deltaTime;
        _pitch -= mouseDelta.y * mouseSensitivity * Time.deltaTime;
        _pitch = Mathf.Clamp(_pitch, -verticalClamp, verticalClamp);
        exteriorCamera.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
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
                DisableCameraControl();
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
        if (_energyStatus == EnergyStatus.Empty)
        {
            _isProcessingPhoto = false;
            yield break;
        }
        EnableCameraControl();
        float totalCooldownTime = cameraPropertyData._VisibleDuration + cameraPropertyData._fadeDuration;
        yield return new WaitForSeconds(totalCooldownTime);
        DisableCameraControl();
        _isProcessingPhoto = false;
    }

    #endregion
    public void EnableCameraControl()
    {
        _isActiveView = true;
        exteriorCamera.enabled = true;

        Vector3 rot = exteriorCamera.transform.eulerAngles;
        _yaw = rot.y;
        _pitch = rot.x;
    }

    public void DisableCameraControl()
    {
        _isActiveView = false;
        exteriorCamera.enabled = false;
    }
}