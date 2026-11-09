
using UnityEngine;

public class SpeedLever : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SubmarineMovement submarineMovement;

    private SubmarineSpeedMode _currentMode = SubmarineSpeedMode.Inactive;

    public SubmarineSpeedMode CurrentMode => _currentMode;

    private void Start()
    {
        if (submarineMovement == null)
        {
            Debug.LogError("[SpeedLever] SubmarineMovement no está asignado.");
            return;
        }

        submarineMovement.SetSpeedMode(SubmarineSpeedMode.Inactive);
    }

    public void SetSpeedMode(SubmarineSpeedMode mode)
    {
        if (submarineMovement == null)
            return;

        submarineMovement.SetSpeedMode(mode);

        _currentMode = submarineMovement.CurrentSpeedMode;

        Debug.Log($"[SpeedLever] Speed mode: {_currentMode}");
    }

    public void SetInactive()
    {
        SetSpeedMode(SubmarineSpeedMode.Inactive);
    }

    public void SetNormal()
    {
        SetSpeedMode(SubmarineSpeedMode.Normal);
    }

    public void SetReverse()
    {
        SetSpeedMode(SubmarineSpeedMode.Reverse);
    }

    public void SetUrgency()
    {
        SetSpeedMode(SubmarineSpeedMode.Urgency);
    }
}
