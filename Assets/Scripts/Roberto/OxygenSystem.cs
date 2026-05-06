using System;
using UnityEngine;

public class OxygenSystem : MonoBehaviour
{
    [Header("Oxygen")]
    [SerializeField] private float maxOxygen = 60f; // 1 minute
    [SerializeField] private float currentOxygen;

    [Header("State")]
    [SerializeField] private bool isDraining = false;
    
    [Header("Events Channels")]
    [SerializeField] private BaseEventChannelSO onDeath;
    [SerializeField] private BoolEventChannelSO onLowOxygen;     // true = oxígeno crítico (≤10%), false = recuperado
    
    public float MaxOxygen { get => maxOxygen;
        private  set => maxOxygen = value;
    }
    public float CurrentOxygen { get => currentOxygen;
        private  set => currentOxygen = value;
    }
    private bool _isLow;

    void Start()
    {
        currentOxygen = maxOxygen;
    }
    void Update()
    {
        if (isDraining) //TODO: Cambiar a Corrutina. Asi no se ejecuta continuamente en caso de que se requiera checkear     su ejecucion de manera mas estable.
        {
            currentOxygen -= Time.deltaTime;

            if (currentOxygen <= 0)
            {
                currentOxygen = 0;
                GameOver(); //TODO: Evitar que se ejecute continuamente.
            }
        }
        CheckLowOxygenThreshold();
    }

    private void CheckLowOxygenThreshold()
    {
        bool low = (currentOxygen / maxOxygen) <= 0.15f;
        if (low == _isLow) return;
        _isLow = low;
        onLowOxygen.RaiseEvent(_isLow);
    }

    public void StartDrain()
    {
        isDraining = true;
        Log.Info("Oxygen Descending...");
    }

    public void StopDrain()
    {
        isDraining = false;
        Log.Info("Oxygen Stabilized");
    }

    public void RestoreOxygen(float amount)
    {
        currentOxygen += amount;
        currentOxygen = Mathf.Clamp(currentOxygen, 0, maxOxygen);
    }

    void GameOver()
    {
        Log.Info("GAME OVER - Oxygen");
        onDeath.RaiseEvent();
    }
}