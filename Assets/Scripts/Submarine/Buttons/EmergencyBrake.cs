
using UnityEngine;

public class EmergencyBrake : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SubmarineMovement submarineMovement;
    [SerializeField] private EngineSystem engineSystem;

    [Header("Temperature Settings")]
    [SerializeField] private float temperatureIncrease = 5f;
    [SerializeField] private float maxAccumulatedIncrease = 15f;

    private float _totalTemperatureIncrease;

    public void ActivateBrake()
    {
        if (submarineMovement == null || engineSystem == null) return;
        if (!engineSystem.IsRunning()) return;
        if (submarineMovement.IsEmergencyBraking) return;
        if (Mathf.Abs(submarineMovement.CurrentSpeed) < 0.1f) return;
        if (!submarineMovement.ActivateEmergencyBrake()) return;
        if (_totalTemperatureIncrease < maxAccumulatedIncrease)
        {
            float amount = Mathf.Min(temperatureIncrease,maxAccumulatedIncrease - _totalTemperatureIncrease);

            engineSystem.IncreaseTemperature(amount);
            _totalTemperatureIncrease += amount;
        }
        Debug.Log("[EmergencyBrake] Emergency brake activated.");
    }
}
