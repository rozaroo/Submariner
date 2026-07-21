using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PhosphorusCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PeriscopeCameraAnchorSO periscopeCameraAnchorSo;
    [SerializeField] private Camera exteriorCamera;

    [Header("Edge Rotation Settings")]
    [Tooltip("Speed Rotation per seconds.")]
    [SerializeField] private float rotationSpeed = 45f;
    [Tooltip("Fraction active for rotation. 0.1 = 10% of the screen width/height.")]
    [Range(0.01f, 0.3f)]
    [SerializeField] private float edgeThreshold = 0.1f;
    [SerializeField] private float verticalClamp = 70f;
    [Tooltip("Camera maximum rotation speed for smoothness. Higher Values = Less Inertia.")]
    [SerializeField] private float rotationSmoothness = 8f; 
    
    private float _currentVelocityX;
    private float _currentVelocityY;
    
    [Header("Zoom Settings")]
    [SerializeField] private float zoomedFOV = 30f;
    [SerializeField] private float zoomTransitionDuration = 0.3f;
    private float _defaultFOV;
    
    [Header("Sequence Timings")]
    [SerializeField] private float flashFadeInDuration = 0.05f;
    [SerializeField] private float whiteScreenDuration = 0.15f;
    [SerializeField] private float exteriorCameraDuration = 2.0f;
    [SerializeField] private float finalFadeOutDuration = 0.75f;

    [Header("Energy Consumption")]
    [SerializeField] private float energyConsumption = 5f;

    private EnergyStatus _energyStatus = EnergyStatus.Full;
    private bool _isProcessingPhoto;
    private bool _isPossessingCamera;
    private bool _hasRegisteredEnergyConsumption;
    
    private float _yaw;
    private float _pitch;
    private Coroutine _photoSequenceRoutine;
    
    public bool IsPossessed => _isPossessingCamera;
    public float CurrentYaw => Mathf.Repeat(_yaw, 360f);
    public float CurrentPitch => -_pitch;
    
    private void Start()
    {
        if (exteriorCamera != null) 
        {
            exteriorCamera.enabled = false;
            _defaultFOV = exteriorCamera.fieldOfView;
        }
        else Log.Warning("[PhosphorusCamera]: No Exterior Camera");
        
        if (periscopeCameraAnchorSo != null) periscopeCameraAnchorSo.phosphorusCameraComponent = this;
        else Log.Warning("[PhosphorusCamera]: No PeriscopeCameraAnchor");
    }
    
    private void Update()
    {
        if (_isPossessingCamera)
        {
            HandleEdgeRotation();
        }
    }
    
    private void OnEnable() => GameEventChannel<OnEnergyStatusChange>.OnEventRaised += UpdateEnergyStatus;
    
    private void OnDisable() => GameEventChannel<OnEnergyStatusChange>.OnEventRaised -= UpdateEnergyStatus;

private void HandleEdgeRotation()
    {
        if (Mouse.current == null) return;
        
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        
        float viewportX = mousePosition.x / Screen.width;
        float viewportY = mousePosition.y / Screen.height;

        float directionX = 0f;
        float directionY = 0f;
        
        if (viewportX >= 0f && viewportX <= 1f)
        {
            if (viewportX < edgeThreshold) directionX = -1f;
            else if (viewportX > 1f - edgeThreshold) directionX = 1f;
        }

        if (viewportY >= 0f && viewportY <= 1f)
        {
            if (viewportY < edgeThreshold) directionY = 1f; 
            else if (viewportY > 1f - edgeThreshold) directionY = -1f; 
        }
        
        float targetVelocityX = directionX * rotationSpeed;
        float targetVelocityY = directionY * rotationSpeed;

        _currentVelocityX = Mathf.Lerp(_currentVelocityX, targetVelocityX, rotationSmoothness * Time.deltaTime);
        _currentVelocityY = Mathf.Lerp(_currentVelocityY, targetVelocityY, rotationSmoothness * Time.deltaTime);
        
        if (Mathf.Abs(_currentVelocityX) < 0.01f) _currentVelocityX = 0f;
        if (Mathf.Abs(_currentVelocityY) < 0.01f) _currentVelocityY = 0f;
        
        if (_currentVelocityX != 0f || _currentVelocityY != 0f)
        {
            _yaw += _currentVelocityX * Time.deltaTime;
            _pitch += _currentVelocityY * Time.deltaTime;
            
            _pitch = Mathf.Clamp(_pitch, -verticalClamp, verticalClamp);
            
            exteriorCamera.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }
    }

    private void UpdateEnergyStatus(OnEnergyStatusChange newStatus)
    {
        _energyStatus = newStatus.energyStatus;
        if (_energyStatus == EnergyStatus.Empty)
        {
            if (_photoSequenceRoutine != null) StopCoroutine(_photoSequenceRoutine);
            StopEnergyConsumption();
            _isProcessingPhoto = false;

            if (_isPossessingCamera && periscopeCameraAnchorSo.flashComponent != null)
                periscopeCameraAnchorSo.flashComponent.SetOverlayColor(Color.black, 1f);
        }
    }

    public bool CanTakePhoto() => _energyStatus != EnergyStatus.Empty && !_isProcessingPhoto;

    public void TryTakePhoto()
    {
        if (!CanTakePhoto()) return;
        
        SFXManager.PostEvent("Start_PhosphorusCameraFlash_Event", gameObject);
        if (_photoSequenceRoutine != null) StopCoroutine(_photoSequenceRoutine);
        _photoSequenceRoutine = StartCoroutine(PhotoSequenceRoutine());
    }
    
    private IEnumerator PhotoSequenceRoutine()
    {
        _isProcessingPhoto = true;
        
        float zoomTimer = 0f;
        while (zoomTimer < zoomTransitionDuration)
        {
            zoomTimer += Time.deltaTime;
            exteriorCamera.fieldOfView = Mathf.Lerp(_defaultFOV, zoomedFOV, zoomTimer / zoomTransitionDuration);
            yield return null;
        }

        StartEnergyConsumption();
        PeriscopeFlash3D flash = periscopeCameraAnchorSo.flashComponent;
        
        if (flash != null) flash.SetOverlayColor(Color.black, 1f);
        
        float timer = 0f;
        while (timer < flashFadeInDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / flashFadeInDuration;
            if (flash != null) flash.SetOverlayColor(Color.white, Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }
        if (flash != null) flash.SetOverlayColor(Color.white, 1f);
        
        yield return new WaitForSeconds(whiteScreenDuration);
        if (flash != null) flash.SetOverlayAlpha(0f); 
        
        yield return new WaitForSeconds(exteriorCameraDuration);
        
        timer = 0f;
        while (timer < finalFadeOutDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / finalFadeOutDuration;
            if (flash != null) flash.SetOverlayColor(Color.black, Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }
        if (flash != null) flash.SetOverlayColor(Color.black, 1f);
        
        exteriorCamera.fieldOfView = _defaultFOV;
        StopEnergyConsumption();
        _isProcessingPhoto = false;
    }

    public void EnableCamera()
    {
        if (periscopeCameraAnchorSo.playerCamera != null) 
        {
            if (exteriorCamera != null)
            {
                exteriorCamera.targetTexture = periscopeCameraAnchorSo.playerCamera.targetTexture;
            }
            periscopeCameraAnchorSo.playerCamera.enabled = false;
        }
        
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

    public void ForceDisable()
    {
        if (_photoSequenceRoutine != null) StopCoroutine(_photoSequenceRoutine);
        StopEnergyConsumption();
        _isProcessingPhoto = false;
        if (exteriorCamera != null) exteriorCamera.fieldOfView = _defaultFOV; 
        
        if (exteriorCamera != null) 
        {
            exteriorCamera.targetTexture = null; 
            exteriorCamera.enabled = false;
        }
        
        if (periscopeCameraAnchorSo.playerCamera != null) 
        {
            periscopeCameraAnchorSo.playerCamera.enabled = true;
        }
            
        if (periscopeCameraAnchorSo.flashComponent != null) 
        {
            periscopeCameraAnchorSo.flashComponent.SetOverlayAlpha(0f);
        }
    }

    public void BeginPeriscopeControl() => _isPossessingCamera = true;
    public void EndPeriscopeControl() => _isPossessingCamera = false;

    private void StartEnergyConsumption()
    {
        if (_hasRegisteredEnergyConsumption)
        {
            return;
        }

        _hasRegisteredEnergyConsumption = true;
        GameEventChannel<OnEnergyConsumption>.RaiseEvent(new OnEnergyConsumption(energyConsumption, true));
        Log.Info($"[PhosphorusCamera] Energy consumption registered: {energyConsumption}");
    }

    private void StopEnergyConsumption()
    {
        if (!_hasRegisteredEnergyConsumption)
        {
            return;
        }

        _hasRegisteredEnergyConsumption = false;
        GameEventChannel<OnEnergyConsumption>.RaiseEvent(new OnEnergyConsumption(energyConsumption, false));
        Log.Info($"[PhosphorusCamera] Energy consumption relieved: {energyConsumption}");
    }
}