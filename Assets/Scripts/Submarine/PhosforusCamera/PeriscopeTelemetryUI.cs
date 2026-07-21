using UnityEngine;
using TMPro;

public class PeriscopeTelemetryUI : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private PeriscopeCameraAnchorSO periscopeAnchorSo;

    [Header("UI Elements")]
    [Tooltip("Parent that contains the objects.")]
    [SerializeField] private GameObject telemetryContainer; 
    [SerializeField] private TextMeshProUGUI yawText;   
    [SerializeField] private TextMeshProUGUI pitchText; 

    private void Awake()
    {
        if (telemetryContainer != null) 
        {
            telemetryContainer.SetActive(false);
        }
    }

    private void OnEnable()
    {
        GameEventChannel<OnPeriscopePossess>.OnEventRaised += ShowTelemetry;
        GameEventChannel<OnPeriscopeUnPossess>.OnEventRaised += HideTelemetry;
    }

    private void OnDisable()
    {
        GameEventChannel<OnPeriscopePossess>.OnEventRaised -= ShowTelemetry;
        GameEventChannel<OnPeriscopeUnPossess>.OnEventRaised -= HideTelemetry;
    }

    private void ShowTelemetry(OnPeriscopePossess evt)
    {
        if (telemetryContainer != null) telemetryContainer.SetActive(true);
    }

    private void HideTelemetry(OnPeriscopeUnPossess evt)
    {
        if (telemetryContainer != null) telemetryContainer.SetActive(false);
    }

    private void Update()
    {
        if (telemetryContainer == null || !telemetryContainer.activeSelf) 
            return;
            
        if (periscopeAnchorSo == null || periscopeAnchorSo.phosphorusCameraComponent == null) 
            return;

        UpdateTelemetry();
    }

    private void UpdateTelemetry()
    {
        float yaw = periscopeAnchorSo.phosphorusCameraComponent.CurrentYaw;
        float pitch = periscopeAnchorSo.phosphorusCameraComponent.CurrentPitch;

        if (yawText != null)
        {
            yawText.text = $"BRG {Mathf.FloorToInt(yaw):D3}°";
        }

        if (pitchText != null)
        {
            string pitchSign = pitch > 0 ? "+" : "";
            pitchText.text = $"ELV {pitchSign}{Mathf.RoundToInt(pitch)}°";
        }
    }
}