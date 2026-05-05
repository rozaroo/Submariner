using UnityEngine;

public class DrainageSystem : MonoBehaviour
{
    [Header("Estado")]
    public bool isActive = false;

    [Header("Agua")]
    public float waterLevel = 100f;
    public float drainSpeed = 20f;
    float drainageEfficiency = 1f;

    [Header("Referencias")]
    public EnergySystem energySystem;

    [Header("Eventos")]
    public DrainagePropertyEventChannelSO onDrainageStarted;
    [SerializeField] private EnergyStatusEventSO onEnergyStatusChanged;

    void OnEnable()
    {
        onDrainageStarted.OnEventRaised += StartDrainage;
        onEnergyStatusChanged.OnEventRaised += UpdateDrainageEfficiency;
    }

    void OnDisable()
    {
        onDrainageStarted.OnEventRaised -= StartDrainage;
        onEnergyStatusChanged.OnEventRaised -= UpdateDrainageEfficiency;
    }

    void Update()
    {
        if (!isActive) return;

        // 🔴 Si energía cayó a 0 mientras drenaba
        if (drainageEfficiency <= 0f)
        {
            isActive = false;
            Debug.Log("⛔ Drenaje detenido por falta de energía");
            return;
        }

        // 💧 Drenaje normal
        if (waterLevel > 0)
        {
            waterLevel -= drainSpeed * drainageEfficiency * Time.deltaTime;
            waterLevel = Mathf.Clamp(waterLevel, 0, 100);
        }

        // ⚠️ Penalización (esto es lo importante del diseño)
        ConsumeEnergy();

        if (waterLevel <= 0) Debug.Log("⚠ Drenaje activo sin agua (penalización)");
        
    }

    void StartDrainage(DrainagePropertyData data)
    {
        drainageEfficiency = data.drainagePercentage;

        // 🔴 BLOQUEO TOTAL
        if (drainageEfficiency <= 0f)
        {
            Debug.Log("⛔ Drenaje BLOQUEADO (sin energía)");
            isActive = false;
            return;
        }
        isActive = true;
        Debug.Log("🟡 Drenaje ACTIVADO | Eficiencia: " + drainageEfficiency);
    }

    public void StopDrainage()
    {
        isActive = false;
        Debug.Log("🔵 Drenaje APAGADO manualmente");
    }

    void ConsumeEnergy()
    {
        if (energySystem != null) energySystem.ConsumeEnergyAmount(5f * Time.deltaTime);
    }
    void UpdateDrainageEfficiency(EnergyStatus status)
    {
        switch (status)
        {
            case EnergyStatus.Full:
                drainageEfficiency = 1f;
                break;

            case EnergyStatus.Low:
                drainageEfficiency = 0.5f;
                break;

            case EnergyStatus.Empty:
                drainageEfficiency = 0f;
                break;
        }

        Debug.Log("⚙ Nueva eficiencia de drenaje: " + drainageEfficiency);
    }
}