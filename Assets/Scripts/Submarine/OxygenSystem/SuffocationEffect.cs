using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SuffocationEffect : MonoBehaviour
{
    [SerializeField] private Volume volume;

    [Header("Vignette")]
    [SerializeField] private float maxIntensity = 0.7f;
    [SerializeField] private float curve = 3f;     // Potencia de la curva: valores altos (3-5) hacen que tarde más en notarse y se dispare al final

    [Header("Pulse")]
    [SerializeField] private float pulseAmplitude = 0.04f;     // Amplitud del titileo sobre el valor base (0 = sin titileo)
    [SerializeField] private float pulseSpeed = 2.5f;     // Velocidad del titileo

    [Header("Recover")]
    [SerializeField] private float recoverySpeed = 0.3f;     // Velocidad a la que se recupera la visión al restaurar oxígeno
    
    [Header("Event Channels")]
    public FloatEventChannelSO onSuffocationProgress;
    
    private Vignette _vignette;
    private float _targetProgress;
    private float _currentProgress;

    private void Awake()
    {
        if (!volume.profile.TryGet(out _vignette)) Log.Error("[SuffocationEffect] El Volume no tiene un override de Vignette.");
        if(onSuffocationProgress  == null) Log.Error("on Suffocation Progress Event Not placed");
    }

    private void OnEnable()
    {
        onSuffocationProgress.OnEventRaised += OnSuffocationProgress;
    }

    private void OnDisable()
    {
        onSuffocationProgress.OnEventRaised -= OnSuffocationProgress;
        _targetProgress = 0f;
    }

    private void OnSuffocationProgress(float currentOxygen)
    {
        _targetProgress = currentOxygen;
    }

    private void Update() //TODO: Cambiar a Corrutina.
    {
        if (_vignette == null) return;

        // Sube instantáneo con la sofocación, baja suavemente al recuperarse
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
