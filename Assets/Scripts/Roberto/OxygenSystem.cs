using System;
using System.Collections;
using UnityEngine;

public class OxygenSystem : MonoBehaviour
{
    [Header("Oxygen")]
    [SerializeField] private float maxOxygen = 60f;
    [SerializeField] private float currentOxygen;

    [Header("Suffocation")]
    [SerializeField] private float suffocationDuration = 60f;
    [SerializeField] private float suffocationTimeRemaining;

    [Header("Event Channels")]
    [SerializeField] private BaseEventChannelSO onDeath;
    [SerializeField] private BoolEventChannelSO onLowOxygen;

    public float MaxOxygen => maxOxygen;
    public float CurrentOxygen => currentOxygen;

    // Progreso de asfixia: 0 = recién sin oxígeno, 1 = muerte
    public Action<float> OnSuffocationProgress;
    public Action<float> OnOxygenChanged;

    private bool _isLow;
    private Coroutine _drainCoroutine;
    private Coroutine _suffocationCoroutine;

    private void Start()
    {
        currentOxygen = maxOxygen;
        StartDrain();
    }

    private IEnumerator DrainCoroutine()
    {
        while (true)
        {
            currentOxygen -= Time.deltaTime;
            OnOxygenChanged?.Invoke(currentOxygen);
            CheckLowOxygenThreshold();

            if (currentOxygen <= 0)
            {
                currentOxygen = 0;
                OnOxygenChanged?.Invoke(currentOxygen);
                _drainCoroutine = null;
                StartSuffocation();
                yield break;
            }

            yield return null;
        }
    }

    private void StartSuffocation()
    {
        if (_suffocationCoroutine != null) return;
        _suffocationCoroutine = StartCoroutine(SuffocationCoroutine());
        Log.Info("Suffocation started...");
    }

    private void StopSuffocation()
    {
        if (_suffocationCoroutine == null) return;
        StopCoroutine(_suffocationCoroutine);
        _suffocationCoroutine = null;
        suffocationTimeRemaining = suffocationDuration;
        OnSuffocationProgress?.Invoke(0f);
    }

    private IEnumerator SuffocationCoroutine()
    {
        float elapsed = 0f;
        suffocationTimeRemaining = suffocationDuration;

        while (elapsed < suffocationDuration)
        {
            elapsed += Time.deltaTime;
            suffocationTimeRemaining = suffocationDuration - elapsed;
            OnSuffocationProgress?.Invoke(elapsed / suffocationDuration);
            yield return null;
        }

        suffocationTimeRemaining = 0f;

        OnSuffocationProgress?.Invoke(1f);
        GameOver();
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
        if (_drainCoroutine != null) return;
        _drainCoroutine = StartCoroutine(DrainCoroutine());
        Log.Info("Oxygen Descending...");
    }

    public void StopDrain()
    {
        if (_drainCoroutine == null) return;
        StopCoroutine(_drainCoroutine);
        _drainCoroutine = null;
        Log.Info("Oxygen Stabilized");
    }

    public void RestoreOxygen(float amount)
    {
        currentOxygen = Mathf.Clamp(currentOxygen + amount, 0, maxOxygen);
        OnOxygenChanged?.Invoke(currentOxygen);
        CheckLowOxygenThreshold();

        // Si se estaba asfixiando, cancelar
        if (_suffocationCoroutine != null)
            StopSuffocation();

        // Reiniciar drenaje si la corrutina había terminado
        if (_drainCoroutine == null && currentOxygen > 0)
            _drainCoroutine = StartCoroutine(DrainCoroutine());
    }

    private void GameOver()
    {
        Log.Info("GAME OVER - Oxygen");
        onDeath.RaiseEvent();
    }
}