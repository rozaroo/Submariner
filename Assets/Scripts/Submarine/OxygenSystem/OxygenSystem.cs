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

    public float CurrentOxygen => currentOxygen;
    public float MaxOxygen => maxOxygen;

    private bool _isLow;
    private Coroutine _drainCoroutine;
    private Coroutine _suffocationCoroutine;

    private void Start()
    {
        currentOxygen = maxOxygen;
        StartDrain();
    }
    
    #region SuffocationLogic

    private void StartSuffocation()
    {
        if (_suffocationCoroutine != null) return;
        _suffocationCoroutine = StartCoroutine(SuffocationCoroutine());
    }

    private void StopSuffocation()
    {
        if (_suffocationCoroutine == null) return;
        StopCoroutine(_suffocationCoroutine);
        _suffocationCoroutine = null;
        suffocationTimeRemaining = suffocationDuration;
        GameEventChannel<OnSuffocationProgressChange>.RaiseEvent(new OnSuffocationProgressChange(0f));
    }

    private IEnumerator SuffocationCoroutine()
    {
        float elapsed = 0f;
        suffocationTimeRemaining = suffocationDuration;

        while (elapsed < suffocationDuration)
        {
            elapsed += Time.deltaTime;
            suffocationTimeRemaining = suffocationDuration - elapsed;
            GameEventChannel<OnSuffocationProgressChange>.RaiseEvent(new OnSuffocationProgressChange(elapsed / suffocationDuration));
            yield return null;
        }

        suffocationTimeRemaining = 0f;

        GameEventChannel<OnSuffocationProgressChange>.RaiseEvent(new OnSuffocationProgressChange(1f));
        OxygenDepleted();
    }

    #endregion

    #region OxygenLogic
    
    private void StartDrain()
    {
        if (_drainCoroutine != null) return;
        _drainCoroutine = StartCoroutine(DrainCoroutine());
        Log.Info("Oxygen Descending...");
    }

    private void StopDrain()
    {
        if (_drainCoroutine == null) return;
        StopCoroutine(_drainCoroutine);
        _drainCoroutine = null;
        Log.Info("Oxygen Stabilized");
    }
    
    private bool _isDrainingPaused;
    
    public void PauseDrain() => _isDrainingPaused = true;
    public void ResumeDrain() => _isDrainingPaused = false;
    
    private IEnumerator DrainCoroutine()
    {
        float accumulatedChange = 0f;
        while (true)
        {
            if (!_isDrainingPaused)
            {
                float drainAmount = Time.deltaTime;
                currentOxygen -= drainAmount;
                accumulatedChange += drainAmount;

                if (accumulatedChange >= 0.5f || currentOxygen <= 0)
                {
                    GameEventChannel<OnOxygenChanged>.RaiseEvent(new OnOxygenChanged(currentOxygen, maxOxygen));
                    CheckLowOxygenThreshold();
                    accumulatedChange = 0f;
                }

                if (currentOxygen <= 0)
                {
                    currentOxygen = 0;
                    _drainCoroutine = null;
                    StartSuffocation();
                    yield break;
                }
            }
            yield return null;
        }
    }

    public void RestoreOxygen(float amount)
    {
        currentOxygen = Mathf.Clamp(currentOxygen + amount, 0, maxOxygen);
        GameEventChannel<OnOxygenChanged>.RaiseEvent(new OnOxygenChanged(currentOxygen, maxOxygen));
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
        GameEventChannel<OnLowOxygen>.RaiseEvent(new OnLowOxygen(_isLow));
    }

    #endregion

    private void OxygenDepleted()
    {
        GameEventChannel<OnDeath>.RaiseEvent(new OnDeath(DeathType.OxygenDepravation));
    }
}