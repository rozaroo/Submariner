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
    [SerializeField] private float suffocationTimeRemaining; //TODO: What? Para que se usa?

    [Header("Event Channels")]
    [SerializeField] private BaseEventChannelSO onDeath;
    [SerializeField] private BoolEventChannelSO onLowOxygen;
    [SerializeField] private FloatEventChannelSO onSuffocationProgress;
    [SerializeField] private OxygenPropertyEventSO onOxygenChanged;

    public float CurrentOxygen => currentOxygen;
    public float MaxOxygen => maxOxygen;

    private bool _isLow;
    private Coroutine _drainCoroutine;
    private Coroutine _suffocationCoroutine;

    private void Start()
    {
        currentOxygen = maxOxygen;
        if(onSuffocationProgress  == null) Log.Error("on Suffocation Progress Event Not placed");
        if(onOxygenChanged  == null) Log.Error("On Oxygen Changed Event Not placed");
        StartDrain();
    }

    private OxygenProperty MakeOxygenProperty()
    {
        return new OxygenProperty
        {
            currentOxygen = this.currentOxygen,
            maxOxygen = this.maxOxygen,
        };
    }
    
    #region SuffocationLogic

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
        onSuffocationProgress?.RaiseEvent(0f);
    }

    private IEnumerator SuffocationCoroutine()
    {
        float elapsed = 0f;
        suffocationTimeRemaining = suffocationDuration;

        while (elapsed < suffocationDuration)
        {
            elapsed += Time.deltaTime;
            suffocationTimeRemaining = suffocationDuration - elapsed;
            onSuffocationProgress.RaiseEvent(elapsed / suffocationDuration);
            yield return null;
        }

        suffocationTimeRemaining = 0f;

        onSuffocationProgress.RaiseEvent(1f);
        OxygenDepleted();
    }

    #endregion

    #region OxygenLogic
    
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
    
    private IEnumerator DrainCoroutine()
    {
        while (true)
        {
            currentOxygen -= Time.deltaTime;
            onOxygenChanged.RaiseEvent(MakeOxygenProperty());
            CheckLowOxygenThreshold();

            if (currentOxygen <= 0)
            {
                currentOxygen = 0;
                onOxygenChanged.RaiseEvent(MakeOxygenProperty());
                _drainCoroutine = null;
                StartSuffocation();
                yield break;
            }
            yield return null;
        }
    }

    public void RestoreOxygen(float amount)
    {
        currentOxygen = Mathf.Clamp(currentOxygen + amount, 0, maxOxygen);
        onOxygenChanged.RaiseEvent(MakeOxygenProperty());
        CheckLowOxygenThreshold();
        
        if (_suffocationCoroutine != null)         // Si se estaba asfixiando, cancelar
            StopSuffocation();
        
        if (_drainCoroutine == null && currentOxygen > 0)         // Reiniciar drenaje si la corrutina había terminado
            _drainCoroutine = StartCoroutine(DrainCoroutine());
    }
    
    private void CheckLowOxygenThreshold()
    {
        bool low = (currentOxygen / maxOxygen) <= 0.15f;
        if (low == _isLow) return;
        _isLow = low;
        onLowOxygen.RaiseEvent(_isLow);
    }

    #endregion

    private void OxygenDepleted()
    {
        Log.Info("GAME OVER - Oxygen");
        onDeath.RaiseEvent();
    }
}