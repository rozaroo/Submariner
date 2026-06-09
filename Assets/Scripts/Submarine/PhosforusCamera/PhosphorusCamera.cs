using System.Collections;
using UnityEngine;
public class PhosphorusCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PeriscopeCameraAnchorSO periscopeCameraAnchorSo;
    [SerializeField] private Camera exteriorCamera;

    [Header("Rotation Settings")]
    [SerializeField] private float mouseSensitivity = 0.4f;
    [SerializeField] private float verticalClamp = 70f;
    
    [Header("Sequence Timings")]
    [SerializeField] private float flashFadeInDuration = 0.05f;
    [SerializeField] private float whiteScreenDuration = 0.15f;
    [SerializeField] private float exteriorCameraDuration = 2.0f;
    [SerializeField] private float finalFadeOutDuration = 0.75f;

    private EnergyStatus _energyStatus = EnergyStatus.Full;
    private bool _isProcessingPhoto;
    private bool _isPossessingCamera;
    private float _yaw;
    private float _pitch;
    private Coroutine _photoSequenceRoutine;
    
    private void Start()
    {
        if (exteriorCamera != null) exteriorCamera.enabled = false;
        else Log.Warning("[PhosphorusCamera]: No Exterior Camera");
        
        if (periscopeCameraAnchorSo != null) periscopeCameraAnchorSo.phosphorusCameraComponent = this;
        else Log.Warning("[PhosphorusCamera]: No PeriscopeCameraAnchor");
    }

    private void OnEnable()
    {
        GameEventChannel<OnEnergyStatusChange>.OnEventRaised += UpdateEnergyStatus;
    }

    private void OnDisable()
    {
        GameEventChannel<OnEnergyStatusChange>.OnEventRaised -= UpdateEnergyStatus;
    } 

    public void Rotate(Vector2 mouseDelta)
    {
        if (!_isPossessingCamera) return;
        _yaw += mouseDelta.x * mouseSensitivity;
        _pitch -= mouseDelta.y * mouseSensitivity;
        _pitch = Mathf.Clamp(_pitch, -verticalClamp, verticalClamp);
        exteriorCamera.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    private void UpdateEnergyStatus(OnEnergyStatusChange newStatus)
    {
        _energyStatus = newStatus.energyStatus;
        Log.Info($"{newStatus} - Phosphorus Camera Status");
        if (_energyStatus == EnergyStatus.Empty)
        {
            ForceDisable();
            Log.Info("Camera Disabled - No Energy");
        }
    }

    public bool CanTakePhoto() => _energyStatus != EnergyStatus.Empty && !_isProcessingPhoto;

    public void TryTakePhoto()
    {
        if (!CanTakePhoto())
        {
            SFXManager.PostEvent("Start_PhosphorusCameraFlash_Event", gameObject);
            return;
        }
        
        if (_photoSequenceRoutine != null) StopCoroutine(_photoSequenceRoutine);
        SFXManager.PostEvent("Start_PhosphorusCameraFlash_Event", gameObject);
        _photoSequenceRoutine = StartCoroutine(PhotoSequenceRoutine());
    }
    
    private IEnumerator PhotoSequenceRoutine()
{
    _isProcessingPhoto = true;
    PeriscopeFlash3D flash = periscopeCameraAnchorSo.flashComponent;
    
    if (flash != null) 
        flash.SetOverlayColor(Color.black, 1f);
    
    float timer = 0f;
    while (timer < flashFadeInDuration)
    {
        timer += Time.deltaTime;
        float progress = timer / flashFadeInDuration;
        
        float smoothWhiteAlpha = Mathf.SmoothStep(0f, 1f, progress); 
        
        if (flash != null) 
            flash.SetOverlayColor(Color.white, smoothWhiteAlpha);
        yield return null;
    }
    if (flash != null) 
        flash.SetOverlayColor(Color.white, 1f);
    
    yield return new WaitForSeconds(whiteScreenDuration);
    
    if (flash != null) flash.SetOverlayAlpha(0f); 
    
    yield return new WaitForSeconds(exteriorCameraDuration);
    
    timer = 0f;
    while (timer < finalFadeOutDuration)
    {
        timer += Time.deltaTime;
        float progress = timer / finalFadeOutDuration;
        
        float smoothBlackAlpha = Mathf.SmoothStep(0f, 1f, progress);
        
        if (flash != null) 
            flash.SetOverlayColor(Color.black, smoothBlackAlpha);
        yield return null;
    }
    if (flash != null) flash.SetOverlayColor(Color.black, 1f);
    
    _isProcessingPhoto = false;
}

    public void EnableCamera()
    {
        if (periscopeCameraAnchorSo.playerCamera != null) 
            periscopeCameraAnchorSo.playerCamera.enabled = false;
        
        if (exteriorCamera != null) exteriorCamera.enabled = true; 
        
        if (periscopeCameraAnchorSo.flashComponent != null)
        {
            periscopeCameraAnchorSo.flashComponent.SetOverlayColor(Color.black, 1f);
        }

        Vector3 rotation = exteriorCamera.transform.eulerAngles;
        _yaw = rotation.y;
        _pitch = rotation.x;
        if (_pitch > 180f) _pitch -= 360f;
    }

    private void DisableCamera()
    {
        if (exteriorCamera != null) exteriorCamera.enabled = false;
        
        if (periscopeCameraAnchorSo.playerCamera != null) 
            periscopeCameraAnchorSo.playerCamera.enabled = true;
            
        if (periscopeCameraAnchorSo.flashComponent != null)
        {
            periscopeCameraAnchorSo.flashComponent.SetOverlayAlpha(0f);
        }
    }

    public void ForceDisable()
    {
        if (_photoSequenceRoutine != null) StopCoroutine(_photoSequenceRoutine);
        _isProcessingPhoto = false;
        DisableCamera();
    }

    public void BeginPeriscopeControl() => _isPossessingCamera = true;
    public void EndPeriscopeControl() => _isPossessingCamera = false;
}