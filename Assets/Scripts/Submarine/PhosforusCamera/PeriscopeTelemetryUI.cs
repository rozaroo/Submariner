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
    [SerializeField] private TextMeshProUGUI cooldownText;
    [SerializeField] private GameObject photoPrompt;
    [SerializeField] private GameObject noBatteryIcon;

    private void Awake()
    {
        if (telemetryContainer != null) telemetryContainer.SetActive(false);
        if (cooldownText != null) cooldownText.gameObject.SetActive(false);
        if (photoPrompt != null) photoPrompt.SetActive(false);
        if (noBatteryIcon != null) noBatteryIcon.SetActive(false);
    }

    private void OnEnable()
    {
        GameEventChannel<OnPeriscopePossess>.OnEventRaised += ShowTelemetry;
        GameEventChannel<OnPeriscopeUnPossess>.OnEventRaised += HideTelemetry;
        GameEventChannel<OnEnergyStatusChange>.OnEventRaised += UpdateBatteryIcon;
    }

    private void OnDisable()
    {
        GameEventChannel<OnPeriscopePossess>.OnEventRaised -= ShowTelemetry;
        GameEventChannel<OnPeriscopeUnPossess>.OnEventRaised -= HideTelemetry;
        GameEventChannel<OnEnergyStatusChange>.OnEventRaised -= UpdateBatteryIcon;
    }

    private void ShowTelemetry(OnPeriscopePossess evt)
    {
        if (telemetryContainer != null) telemetryContainer.SetActive(true);
        if (photoPrompt != null) photoPrompt.SetActive(true);
        if (noBatteryIcon != null) noBatteryIcon.SetActive(false);
    }

    private void HideTelemetry(OnPeriscopeUnPossess evt)
    {
        if (telemetryContainer != null) telemetryContainer.SetActive(false);
        if (photoPrompt != null) photoPrompt.SetActive(false);
        if (noBatteryIcon != null) noBatteryIcon.SetActive(false);
    }

    private void Update()
    {
        if (telemetryContainer == null || !telemetryContainer.activeSelf) return;
        if (periscopeAnchorSo == null || periscopeAnchorSo.phosphorusCameraComponent == null) return;
        UpdateTelemetry();
    }

    private void UpdateTelemetry()
    {
        float yaw = periscopeAnchorSo.phosphorusCameraComponent.CurrentYaw;
        float pitch = periscopeAnchorSo.phosphorusCameraComponent.CurrentPitch;
        if (yawText != null) yawText.text = $"BRG {Mathf.FloorToInt(yaw):D3}°";
        if (pitchText != null)
        {
            string pitchSign = pitch > 0 ? "+" : "";
            pitchText.text = $"ELV {pitchSign}{Mathf.RoundToInt(pitch)}°";
        }
        if (cooldownText != null)
        {
            float cooldown = periscopeAnchorSo.phosphorusCameraComponent.CooldownRemaining;
            if (cooldown > 0f)
            {
                cooldownText.gameObject.SetActive(true);
                cooldownText.text = $"COOLDOWN {cooldown:F1}s";
            }
            else cooldownText.gameObject.SetActive(false);
        }
        if (photoPrompt != null)
        {
            bool canTakePhoto = periscopeAnchorSo.phosphorusCameraComponent.CanTakePhoto();
            photoPrompt.SetActive(canTakePhoto);
        }
    }
    private void UpdateBatteryIcon(OnEnergyStatusChange newStatus)
    {
        if (noBatteryIcon == null) return;
        bool batteryEmpty = newStatus.energyStatus == EnergyStatus.Empty;
        noBatteryIcon.SetActive(batteryEmpty);
    }
}