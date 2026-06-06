using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SuffocationEffect : MonoBehaviour
{
    [SerializeField] private Volume volume;

    [Header("Vignette")]
    [SerializeField] private float maxIntensity = 0.7f;
    [SerializeField] private float curve = 3f;

    [Header("Pulse")]
    [SerializeField] private float pulseAmplitude = 0.04f;
    [SerializeField] private float pulseSpeed = 2.5f;

    [Header("Recover")]
    [SerializeField] private float recoverySpeed = 0.3f;
    
    private Vignette _vignette;
    private float _targetProgress;
    private float _currentProgress;

    private void Awake()
    {
        if (!volume.profile.TryGet(out _vignette)) Log.Error("[SuffocationEffect] El Volume no tiene un override de Vignette.");
    }

    private void OnEnable()
    {
        GameEventChannel<OnSuffocationProgressChange>.OnEventRaised += OnSuffocationProgress;
    }

    private void OnDisable()
    {
        GameEventChannel<OnSuffocationProgressChange>.OnEventRaised -= OnSuffocationProgress;
        _targetProgress = 0f;
    }

    private void OnSuffocationProgress(OnSuffocationProgressChange e)
    {
        _targetProgress = e.currentSuffocationProgress;
    }

    private void Update() //TODO: Cambiar a Corrutina.
    {
        if (_vignette == null) return;
        
        if (_targetProgress > _currentProgress)
            _currentProgress = _targetProgress;
        else
            _currentProgress = Mathf.MoveTowards(_currentProgress, _targetProgress, recoverySpeed * Time.deltaTime);

        if (_currentProgress <= 0f)
        {
            _vignette.intensity.Override(0f);
            return;
        }
        
        float baseIntensity = maxIntensity * Mathf.Pow(_currentProgress, curve);
        float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude * _currentProgress;
        _vignette.intensity.Override(Mathf.Clamp01(baseIntensity + pulse));
    }
}
