using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Colocar en cualquier GameObject de la escena.
// Asignar el OxygenSystem y el Volume global que tenga un override de Vignette.
public class SuffocationEffect : MonoBehaviour
{
    [SerializeField] private OxygenSystem oxygenSystem;
    [SerializeField] private Volume volume;

    [Header("Viñeta")]
    [SerializeField] private float maxIntensity = 0.7f;
    // Potencia de la curva: valores altos (3-5) hacen que tarde más en notarse y se dispare al final
    [SerializeField] private float curve = 3f;

    [Header("Pulso")]
    // Amplitud del titileo sobre el valor base (0 = sin titileo)
    [SerializeField] private float pulseAmplitude = 0.04f;
    // Velocidad del titileo
    [SerializeField] private float pulseSpeed = 2.5f;

    [Header("Recuperación")]
    // Velocidad a la que se recupera la visión al restaurar oxígeno
    [SerializeField] private float recoverySpeed = 0.3f;

    private Vignette _vignette;
    private float _targetProgress;
    private float _currentProgress;

    private void Awake()
    {
        if (!volume.profile.TryGet(out _vignette))
            Debug.LogError("[SuffocationEffect] El Volume no tiene un override de Vignette.");
    }

    private void OnEnable()
    {
        oxygenSystem.OnSuffocationProgress += OnSuffocationProgress;
    }

    private void OnDisable()
    {
        oxygenSystem.OnSuffocationProgress -= OnSuffocationProgress;
        _targetProgress = 0f;
    }

    private void OnSuffocationProgress(float progress)
    {
        _targetProgress = progress;
    }

    private void Update()
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
