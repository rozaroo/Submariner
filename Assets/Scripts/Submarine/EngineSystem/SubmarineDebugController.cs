using UnityEngine;
using UnityEngine.InputSystem;

public class SubmarineDebugController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EngineSystem engineSystem;
    [SerializeField] private SubmarineMovement submarineMovement;
    [SerializeField] private EmergencyBrake emergencyBrake;

    [Header("Angle Settings")]
    [SerializeField] private float angleStep = 5f;

    private void Update()
    {
        if (Keyboard.current == null) return;

        HandleEngineInput();
        HandleSpeedInput();
        HandleAngleInput();
        HandleBrakeInput();
    }

    private void HandleEngineInput()
    {
        if (engineSystem == null) return;

        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            engineSystem.DebugStartEngine();
            Debug.Log("[DEBUG] Engine start requested.");
        }

        if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            engineSystem.DebugStopEngine();
            Debug.Log("[DEBUG] Engine stop requested.");
        }

        if (Keyboard.current.f3Key.wasPressedThisFrame)
        {
            engineSystem.DebugForceCriticalTemperature();
            Debug.Log("[DEBUG] Critical temperature forced.");
        }

        if (Keyboard.current.f4Key.wasPressedThisFrame)
        {
            engineSystem.DebugForceMaxTemperature();
            Debug.Log("[DEBUG] Maximum temperature forced.");
        }
    }

    private void HandleSpeedInput()
    {
        if (submarineMovement == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            SetSpeed(SubmarineSpeedMode.Inactive);

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            SetSpeed(SubmarineSpeedMode.Normal);

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            SetSpeed(SubmarineSpeedMode.Reverse);

        if (Keyboard.current.digit4Key.wasPressedThisFrame)
            SetSpeed(SubmarineSpeedMode.Urgency);
    }

    private void SetSpeed(SubmarineSpeedMode mode)
    {
        submarineMovement.SetSpeedMode(mode);

        Debug.Log(
            $"[DEBUG] Speed Mode: {submarineMovement.CurrentSpeedMode} | " +
            $"Current Speed: {submarineMovement.CurrentSpeed:F2} m/s"
        );
    }

    private void HandleAngleInput()
    {
        if (submarineMovement == null) return;

        if (Keyboard.current.qKey.wasPressedThisFrame)
            ChangeAngle(-angleStep);

        if (Keyboard.current.eKey.wasPressedThisFrame)
            ChangeAngle(angleStep);

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            submarineMovement.SetAngle(0f);
            Debug.Log("[DEBUG] Angle reset to 0 degrees.");
        }
    }

    private void ChangeAngle(float amount)
    {
        float newAngle = submarineMovement.CurrentAngle + amount;

        submarineMovement.SetAngle(newAngle);

        Debug.Log(
            $"[DEBUG] Navigation Angle: {submarineMovement.CurrentAngle}°"
        );
    }

    private void HandleBrakeInput()
{
    if (emergencyBrake == null || submarineMovement == null) return;
    if (Keyboard.current.spaceKey.wasPressedThisFrame)
    {
        bool wasBraking = submarineMovement.IsEmergencyBraking;
        emergencyBrake.ActivateBrake();
        bool isBraking = submarineMovement.IsEmergencyBraking;
        Debug.Log(!wasBraking && isBraking ? "[DEBUG] Emergency brake activated." : "[DEBUG] Emergency brake activation denied.");
    }
}
}
