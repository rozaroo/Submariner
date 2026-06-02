using System.Collections;
using UnityEngine;

public class PhosphorusCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PeriscopeCameraAnchorSO periscopeCameraAnchorSo;
    [SerializeField] private Camera exteriorCamera;

    [Header("Camera Settings")]
    [SerializeField] private CameraPropertyData cameraPropertyData;

    [Header("Rotation Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalClamp = 70f;

    [Header("Event Channels")]
    [SerializeField] private EnergyStatusEventSO energyStatusEventSo;

    [Header("Player Camera")]
    [SerializeField] private Camera mainCamera;

    private EnergyStatus _energyStatus = EnergyStatus.Full;
    private bool _isProcessingPhoto;
    private bool _isPossessingCamera;
    private float _yaw;
    private float _pitch;
    
    #region Startup

    private void Start()
    {
        if (exteriorCamera != null)
        {
            exteriorCamera.enabled = false;
        }
        else
        {
            Log.Warning("[PhosphorusCamera]: No Exterior Camera");
        }
        
        if (periscopeCameraAnchorSo != null)
        {
            periscopeCameraAnchorSo.phosphorusCameraComponent = this;
        }
        else
        {
            Log.Warning("[PhosphorusCamera]: No PeriscopeCameraAnchor");
        }
    }

    private void OnEnable()
    {
        if (energyStatusEventSo != null) energyStatusEventSo.OnEventRaised += UpdateEnergyStatus;
    }

    private void OnDisable()
    {
        if (energyStatusEventSo != null) energyStatusEventSo.OnEventRaised -= UpdateEnergyStatus;
    }

    #endregion

    public void Rotate(Vector2 mouseDelta)
    {
        if (!_isPossessingCamera) return;
        _yaw += mouseDelta.x * mouseSensitivity;
        _pitch -= mouseDelta.y * mouseSensitivity;
        _pitch = Mathf.Clamp(_pitch, -verticalClamp, verticalClamp);
        exteriorCamera.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }


    #region Energy

    private void UpdateEnergyStatus(EnergyStatus newStatus)
    {
        _energyStatus = newStatus;

        if (_energyStatus == EnergyStatus.Empty)
        {
            ForceDisable();
            Log.Info("Camera Disabled - No Energy");
        }
    }

    #endregion

    #region Camera Logic

    public void TryTakePhoto()
    {
        if (_energyStatus == EnergyStatus.Empty)
        {
            Log.Info("No Energy");
            return;
        }
        if (_isProcessingPhoto) return;
        StartCoroutine(CameraRoutine());
    }

    private IEnumerator CameraRoutine()
    {
        _isProcessingPhoto = true;
        yield return new WaitForSeconds(cameraPropertyData._VisibleDuration);
        _isProcessingPhoto = false;
    }

    public void EnableCamera()
    {
        if (mainCamera != null) mainCamera.enabled = false;
        exteriorCamera.enabled = true;
        Vector3 rotation = exteriorCamera.transform.eulerAngles;
        _yaw = rotation.y;
        _pitch = rotation.x;
        if (_pitch > 180f) _pitch -= 360f;
    }

    private void DisableCamera()
    {
        exteriorCamera.enabled = false;
        if (mainCamera != null) mainCamera.enabled = true;
    }
    public void ForceDisable()
    {
        StopAllCoroutines();
        _isProcessingPhoto = false;
        DisableCamera();
    }

    public bool CanTakePhoto()
    {
        return _energyStatus != EnergyStatus.Empty && !_isProcessingPhoto;
    }
    public void BeginPeriscopeControl()
    {
        _isPossessingCamera = true;
    }

    public void EndPeriscopeControl()
    {
        _isPossessingCamera = false;
    }
    #endregion
    
    /*
    public float GetVisibleDuration() //TODO: Not Used Yet, but keep just in case.
    {
        return cameraPropertyData._VisibleDuration;
    }
    */

}