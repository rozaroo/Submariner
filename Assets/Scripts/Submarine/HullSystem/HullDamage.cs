using System;
using UnityEngine;

public class HullDamage : MonoBehaviour
{
    [Header("Repair Properties")]
    [SerializeField] private float repairDuration = 3f;

    [Header("Water Effect")]
    [SerializeField] private ParticleSystem waterLeak;

    public event Action<HullDamage> OnCrackRepaired;
    [SerializeField] private float _progress;

    public void Repair(float amount, Blowtorch activeTorch)
    {
        _progress += amount;
        UpdateLeakIntensity();

        if (_progress >= repairDuration)
        {
            if (activeTorch != null) activeTorch.StopWeldingEffects();
            Close();
        }
    }
    
    private void UpdateLeakIntensity()
    {
        if (waterLeak == null) return;
        float remaining = 1f - Mathf.Clamp01(_progress / repairDuration);
        var emission = waterLeak.emission;
        emission.rateOverTimeMultiplier = baseEmissionRate * remaining;
    }

    private float baseEmissionRate;

    private void Close()
    {
        OnCrackRepaired?.Invoke(this);
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        _progress = 0f;
        if (waterLeak != null)
        {
            if (baseEmissionRate <= 0f)
                baseEmissionRate = waterLeak.emission.rateOverTimeMultiplier;
            waterLeak.Clear(true);
            waterLeak.Play(true);
            UpdateLeakIntensity();
        }
        SFXManager.PostEvent("Start_HullBreachSFX", gameObject);
    }

    private void OnDisable()
    {
        if (waterLeak != null) waterLeak.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        SFXManager.PostEvent("Stop_HullBreachSFX", gameObject);
    }
}