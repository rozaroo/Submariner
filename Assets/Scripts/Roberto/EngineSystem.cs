using UnityEngine;
using UnityEngine.InputSystem;

public class EngineSystem : MonoBehaviour
{
    //TODO: Eliminar Submarine.
    public Submarine submarine;
    public InputActionReference toggleEngineAction;
    public bool engineOn = false;
    [Header("Ruido")]
    public float noiseLevel = 0f;
    public float maxNoise = 10f;

    [Header("Referencias")]
    //public OxygenSystem oxygenSystem;
    public AudioSource engineAudio;

    [Header("Energia")]
    public EnergySystem energySystem;

    void OnEnable() => toggleEngineAction.action.Enable();
    void OnDisable() => toggleEngineAction.action.Disable();

    
    //TODO: Cambiar a eventos de energía para evitar chequeos constantes.
    void Update()
    { 
        if (toggleEngineAction.action.triggered) ToggleEngine();
        CheckEnergyState();
        CheckLowEnergy();
        HandleAudioFeedback();
    }
    void ToggleEngine()
    {
        engineOn = !engineOn;
        if (engineOn) energySystem.SendMessage("StartEnergyConsumption");
        else energySystem.SendMessage("StopEnergyConsumption");
        ApplyEngineState();
        Debug.Log(engineOn ? "Motor ENCENDIDO " : "Motor APAGADO ");
    }
    
    //TODO: Cambiar EngineState a un Enum, en caso de añadir mas estados en algun futuro y a su vez, hacer un statemachine de ser necesario.
    void ApplyEngineState()
    {
        if (engineOn)
        {
            submarine.SetEngineState(true);
            noiseLevel = maxNoise;
            //if (oxygenSystem != null) oxygenSystem.EnablePurifiers(true);
            if (engineAudio && !engineAudio.isPlaying) engineAudio.Play();
        }
        else
        {
            submarine.SetEngineState(false);
            noiseLevel = 0f;
            //if (oxygenSystem != null) oxygenSystem.EnablePurifiers(false);
            if (engineAudio && engineAudio.isPlaying) engineAudio.Stop();
        }
    }
    void CheckEnergyState()
    {
        if (energySystem.GetCurrentEnergy() <= 0 && engineOn)
        {
            engineOn = false;
            energySystem.SendMessage("StopEnergyConsumption");
            ApplyEngineState();
            Debug.Log("ENERGÍA AGOTADA - APAGÓN 🔴");
        }
    }
    void CheckLowEnergy()
    {
        float energyPercent = energySystem.GetCurrentEnergyPercentage();
        if (energyPercent <= 20f && energyPercent > 0) submarine.SetSpeedMultiplier(0.5f);
        else submarine.SetSpeedMultiplier(1f);
    }
    void HandleAudioFeedback()
    {
        if (engineAudio == null) return;
        float energyPercent = energySystem.GetCurrentEnergyPercentage();
        if (energyPercent <= 20f && energyPercent > 0) engineAudio.pitch = 0.6f; // sonido “moribundo”
        else engineAudio.pitch = 1f;
    }
}