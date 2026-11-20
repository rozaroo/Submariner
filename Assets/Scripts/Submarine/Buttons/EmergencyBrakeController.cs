using UnityEngine;

public class EmergencyBrakeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ButtonStation buttonStation;
    [SerializeField] private EmergencyBrake emergencyBrake;

    private void Awake()
    {
        if (buttonStation == null) buttonStation = GetComponent<ButtonStation>();
        if (buttonStation == null) Debug.LogError("[EmergencyBrakeController] ButtonStation no está asignado.", this);
        if (emergencyBrake == null) Debug.LogError("[EmergencyBrakeController] EmergencyBrake no está asignado.", this);
    }

    private void OnEnable()
    {
        if (buttonStation == null || emergencyBrake == null) return;
        buttonStation.onActivation += emergencyBrake.ActivateBrake;
    }

    private void OnDisable()
    {
        if (buttonStation == null || emergencyBrake == null) return;
        buttonStation.onActivation -= emergencyBrake.ActivateBrake;
    }
}
