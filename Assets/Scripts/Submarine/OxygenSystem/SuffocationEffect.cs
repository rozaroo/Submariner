using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SuffocationEffect : MonoBehaviour
{
    private const float BuildUpSpeed = 3f;

    [SerializeField] private Volume volume;

    [Header("Oxygen warning")]
    [SerializeField, Range(0.01f, 1f)] private float lowOxygenThreshold = 0.15f;

    [Header("Vignette")]
    [SerializeField, Range(0f, 1f)] private float maxIntensity = 0.28f;
    [SerializeField, Range(0.01f, 1f)] private float vignetteSmoothness = 0.55f;

    [Header("Suffocation blinks")]
    [SerializeField, Min(0f)] private float blinkStartSeconds = 18f;
    [SerializeField, Range(0f, 10f)] private float blinkExposureDrop = 6f;

    [Header("Transition")]
    [SerializeField, Min(0.01f)] private float recoverySpeed = 0.8f;

    private ColorAdjustments _colorAdjustments;
    private Vignette _vignette;
    private float _baseSaturation;
    private float _baseExposure;
    private float _baseVignetteIntensity;
    private float _baseVignetteSmoothness;
    private bool _baseSaturationOverride;
    private bool _baseExposureOverride;
    private bool _baseVignetteIntensityOverride;
    private bool _baseVignetteSmoothnessOverride;
    private float _lowOxygenProgress;
    private float _suffocationProgress;
    private float _suffocationSecondsRemaining;
    private float _desaturationWeight;
    private float _vignetteWeight;
    private float _blinkWeight;
    private bool _isFinalBlink;
    private float _finalBlinkStartWeight;

    private void Awake()
    {
        if (volume == null) return;

        VolumeProfile profile = volume.profile;
        if (!profile.TryGet(out _colorAdjustments))
            _colorAdjustments = profile.Add<ColorAdjustments>();
        _baseSaturation = _colorAdjustments.saturation.value;
        _baseSaturationOverride = _colorAdjustments.saturation.overrideState;
        _baseExposure = _colorAdjustments.postExposure.value;
        _baseExposureOverride = _colorAdjustments.postExposure.overrideState;

        if (!profile.TryGet(out _vignette))
            _vignette = profile.Add<Vignette>();
        _baseVignetteIntensity = _vignette.intensity.value;
        _baseVignetteSmoothness = _vignette.smoothness.value;
        _baseVignetteIntensityOverride = _vignette.intensity.overrideState;
        _baseVignetteSmoothnessOverride = _vignette.smoothness.overrideState;
    }

    private void OnEnable()
    {
        GameEventChannel<OnOxygenChanged>.OnEventRaised += OnOxygenChanged;
        GameEventChannel<OnSuffocationProgressChange>.OnEventRaised += OnSuffocationProgress;
    }

    private void OnDisable()
    {
        GameEventChannel<OnOxygenChanged>.OnEventRaised -= OnOxygenChanged;
        GameEventChannel<OnSuffocationProgressChange>.OnEventRaised -= OnSuffocationProgress;
        _lowOxygenProgress = 0f;
        _suffocationProgress = 0f;
        _desaturationWeight = 0f;
        _vignetteWeight = 0f;
        _blinkWeight = 0f;
        _isFinalBlink = false;
        ApplyEffect();
    }

    private void OnOxygenChanged(OnOxygenChanged change)
    {
        float oxygenRatio = change.maxOxygen > 0f ? change.currentOxygen / change.maxOxygen : 0f;
        _lowOxygenProgress = 1f - Mathf.InverseLerp(0f, lowOxygenThreshold, oxygenRatio);
    }

    private void OnSuffocationProgress(OnSuffocationProgressChange change)
    {
        _suffocationProgress = Mathf.Clamp01(change.currentSuffocationProgress);
        _suffocationSecondsRemaining = Mathf.Max(0f, change.secondsRemaining);
    }

    private void LateUpdate()
    {
        if (_isFinalBlink) return;

        float desaturationTarget = _suffocationProgress > 0f ? 1f : _lowOxygenProgress;
        float vignetteTarget = Mathf.Lerp(desaturationTarget * 0.4f, 1f, _suffocationProgress);
        float nextDesaturation = MoveWeight(_desaturationWeight, desaturationTarget);
        float nextVignette = MoveWeight(_vignetteWeight, vignetteTarget);
        bool isBlinking = _suffocationProgress > 0f && blinkStartSeconds > 0f
                          && _suffocationSecondsRemaining <= blinkStartSeconds;
        float nextBlink = _blinkWeight;
        if (_suffocationProgress < 1f)
        {
            nextBlink = isBlinking
                ? GetBlinkWeight()
                : Mathf.MoveTowards(_blinkWeight, 0f, 6f * Time.deltaTime);
        }
        if (nextDesaturation == _desaturationWeight && nextVignette == _vignetteWeight && nextBlink == _blinkWeight)
            return;

        _desaturationWeight = nextDesaturation;
        _vignetteWeight = nextVignette;
        _blinkWeight = nextBlink;
        ApplyEffect();
    }

    public bool TryBeginFinalBlink()
    {
        if (!isActiveAndEnabled || _colorAdjustments == null || _vignette == null) return false;

        _finalBlinkStartWeight = _blinkWeight;
        _isFinalBlink = true;
        return true;
    }

    public void SetFinalBlinkProgress(float progress)
    {
        if (!_isFinalBlink) return;

        _blinkWeight = Mathf.Lerp(_finalBlinkStartWeight, 1f, progress);
        ApplyEffect();
    }

    private float GetBlinkWeight()
    {
        float elapsed = blinkStartSeconds - _suffocationSecondsRemaining;
        float urgency = Mathf.Clamp01(elapsed / blinkStartSeconds);
        const float startRate = 1f / 4.2f;
        float rate = Mathf.Lerp(startRate, 1f / 2.7f, urgency);
        // Integrate the increasing blink rate so the cadence speeds up without jumping.
        float cycle = Mathf.Repeat(elapsed * (startRate + rate) * 0.5f, 1f);
        float blinkDuration = Mathf.Lerp(0.65f, 1f, urgency);
        float blinkPhase = cycle / (rate * blinkDuration);
        float close = Mathf.SmoothStep(0f, 1f, blinkPhase / 0.24f);
        float reopen = Mathf.SmoothStep(1f, 0f, (blinkPhase - 0.40f) / 0.60f);
        return close * reopen;
    }

    private float MoveWeight(float current, float target)
    {
        float speed = current < target ? BuildUpSpeed : recoverySpeed;
        return Mathf.MoveTowards(current, target, speed * Time.deltaTime);
    }

    private void ApplyEffect()
    {
        if (_colorAdjustments != null)
        {
            _colorAdjustments.saturation.value = Mathf.Lerp(_baseSaturation, -100f, _desaturationWeight);
            _colorAdjustments.saturation.overrideState = _desaturationWeight > 0f || _baseSaturationOverride;
            _colorAdjustments.postExposure.value = _baseExposure - blinkExposureDrop * _blinkWeight;
            _colorAdjustments.postExposure.overrideState = _blinkWeight > 0f || _baseExposureOverride;
        }

        if (_vignette != null)
        {
            float intensity = Mathf.Lerp(_baseVignetteIntensity, maxIntensity, _vignetteWeight);
            _vignette.intensity.value = Mathf.Lerp(intensity, Mathf.Max(intensity, 0.64f), _blinkWeight);
            _vignette.intensity.overrideState = _vignetteWeight > 0f || _blinkWeight > 0f || _baseVignetteIntensityOverride;
            _vignette.smoothness.value = Mathf.Lerp(_baseVignetteSmoothness, vignetteSmoothness, _vignetteWeight);
            _vignette.smoothness.overrideState = _vignetteWeight > 0f || _baseVignetteSmoothnessOverride;
        }
    }
}
