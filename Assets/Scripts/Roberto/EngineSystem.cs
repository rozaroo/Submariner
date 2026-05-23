using UnityEngine;
using UnityEngine.InputSystem;

public enum EngineState
{
    Off,
    On,
    LowPower,
    Blackout
}
public class EngineSystem : MonoBehaviour
{
    public SubmarineController submarine;
    public InputActionReference toggleEngineAction;
    public bool engineOn = false;
    [Header("Ruido")]
    public float noiseLevel = 0f;
    public float maxNoise = 10f;

    [Header("Referencias")]
    public OxygenSystem oxygenSystem;
    public AudioSource engineAudio;

    [Header("Energia")]
    public EnergySystem energySystem;
    EnergyStatus lastEnergyStatus;
    void OnEnable() => toggleEngineAction.action.Enable();
    void OnDisable() => toggleEngineAction.action.Disable();

    void Start()
    {
        lastEnergyStatus = (EnergyStatus)(-1); // fuerza primera actualización
    }

    void Update()
    {
        if (toggleEngineAction.action.triggered) ToggleEngine();
        if (energySystem == null) return;
        float energyPercent = energySystem.GetCurrentEnergyPercentage();
        CheckEnergyState();
        CheckLowEnergy(energyPercent);
        HandleAudioFeedback(energyPercent);
    }
    void ToggleEngine()
    {
        engineOn = !engineOn;
        if (engineOn) energySystem.StartConsumption();
        else energySystem.StopConsumption();
        ApplyEngineState();
        Debug.Log(engineOn ? "Motor ENCENDIDO" : "Motor APAGADO");
    }

    //TODO: Cambiar EngineState a un Enum, en caso de añadir mas estados en algun futuro y a su vez, hacer un statemachine de ser necesario.
    void ApplyEngineState()
    {
        if (engineOn)
        {
            submarine.SetEngineState(true);
            noiseLevel = maxNoise;
            if (oxygenSystem != null) oxygenSystem.StopDrain(); // motor ON seguro
            if (engineAudio && !engineAudio.isPlaying) engineAudio.Play();
        }
        else
        {
            submarine.SetEngineState(false);
            noiseLevel = 0f;
            if (oxygenSystem != null) oxygenSystem.StartDrain(); // apagón peligro
            if (engineAudio && engineAudio.isPlaying) engineAudio.Stop();
        }
    }
    void CheckEnergyState()
    {
        if (energySystem == null) return;
        if (energySystem.GetCurrentEnergy() <= 0 && engineOn)
        {
            engineOn = false;
            energySystem.StopConsumption(); // directo
            ApplyEngineState();
            Debug.Log("ENERGÍA AGOTADA - APAGÓN");
        }
    }
    void CheckLowEnergy(float energyPercent)
    {
        if (energySystem == null) return;
        EnergyStatus currentStatus;
        if (energyPercent <= 0f) currentStatus = EnergyStatus.Empty;
        else if (energyPercent <= 20f) currentStatus = EnergyStatus.Low;
        else currentStatus = EnergyStatus.Full;
        if (currentStatus == lastEnergyStatus) return;
        lastEnergyStatus = currentStatus;
        switch (currentStatus)
        {
            case EnergyStatus.Low:
                submarine.SetSpeedMultiplier(0.5f);
                break;

            case EnergyStatus.Full:
                submarine.SetSpeedMultiplier(1f);
                break;
        }
    }
    void HandleAudioFeedback(float energyPercent)
    {
        if (energySystem == null) return;
        if (engineAudio == null) return;
        if (energyPercent <= 20f && energyPercent > 0) engineAudio.pitch = 0.6f; // sonido “moribundo”
        else engineAudio.pitch = 1f;
    }
}