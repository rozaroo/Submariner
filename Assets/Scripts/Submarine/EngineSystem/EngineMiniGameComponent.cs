using System.Collections;
using UnityEngine;

public class EngineMiniGameComponent : MonoBehaviour, IInteractable
{
    [SerializeField] private EngineMiniGame engineMinigame;

    [Header("Visual Feedback")]
    [SerializeField] private Renderer tubeRenderer;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color sequenceColor = Color.red;
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color roundTransitionColor = Color.yellow;
    [SerializeField] private float normalEmission = 0f;
    [SerializeField] private float feedbackEmission = 1.5f;
    [SerializeField, Min(1f)] private float surfaceGlowMultiplier = 1.6f;
    [SerializeField, Min(0f)] private float materialEmissionIntensity = 2f;
    [SerializeField] private Light glowLight;
    [SerializeField] private bool createGlowLightIfMissing = true;
    [SerializeField, Min(0f)] private float glowLightIntensity = 1.2f;
    [SerializeField, Min(0.01f)] private float glowLightRange = 0.6f;
    [SerializeField, Min(0.01f)] private float colorTransitionDuration = 0.12f;
    [SerializeField, Min(0.01f)] private float hitBlinkDuration = 0.1f;
    [SerializeField, Min(1)] private int hitBlinkCount = 2;

    private Material _tubeMaterial;
    private bool _isShowingSequence;
    private bool _isCorrect;
    private bool _isHovered;
    private bool _isFailure;
    private bool _isRoundTransition;
    private bool _hideFeedbackForBlink;
    private Coroutine _colorTransitionCoroutine;
    private Coroutine _blinkCoroutine;

    private static readonly int ColorProperty = Shader.PropertyToID("_Color");
    private static readonly int EmissionProperty = Shader.PropertyToID("_Emission");
    private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        if (tubeRenderer == null) return;

        _tubeMaterial = tubeRenderer.material;
        EnsureGlowLight();
        ApplyVisual(immediate: true);
    }

    private void OnDisable()
    {
        if (_colorTransitionCoroutine != null) StopCoroutine(_colorTransitionCoroutine);
        if (_blinkCoroutine != null) StopCoroutine(_blinkCoroutine);
    }

    public void Interact(PlayerCharacter player)
    {
        if (engineMinigame == null)
        {
            Debug.LogError($"[ENGINE MINIGAME] {name}: EngineMinigame no está asignado.");
            return;
        }

        engineMinigame.OnComponentInteracted(this);
    }

    public void ClickInteract(PlayerCharacter player) => Interact(player);

    public void ShowSequenceFeedback()
    {
        ClearTransientFeedback();
        _isShowingSequence = true;
        ApplyVisual();
    }

    public void ShowCorrectFeedback()
    {
        ClearTransientFeedback();
        _isCorrect = true;
        ApplyVisual();
        StartBlinkFeedback();
    }

    public void ShowCompletionFeedback()
    {
        ClearTransientFeedback();
        _isCorrect = true;
        ApplyVisual();
    }

    public void ShowFailureFeedback()
    {
        ClearTransientFeedback();
        _isFailure = true;
        ApplyVisual();
        StartBlinkFeedback();
    }

    public void ShowRoundTransitionFeedback()
    {
        ClearTransientFeedback();
        _isRoundTransition = true;
        ApplyVisual();
    }

    public void TurnOffFeedback()
    {
        StopBlinkFeedback();
        _isShowingSequence = false;
        _isCorrect = false;
        _isFailure = false;
        _isRoundTransition = false;
        ApplyVisual();
    }

    public void SetHovered(bool isHovered)
    {
        if (_isHovered == isHovered) return;

        _isHovered = isHovered;
        ApplyVisual();
    }

    private void ClearTransientFeedback()
    {
        StopBlinkFeedback();
        _isShowingSequence = false;
        _isCorrect = false;
        _isFailure = false;
        _isRoundTransition = false;
    }

    private void StartBlinkFeedback()
    {
        StopBlinkFeedback();
        _blinkCoroutine = StartCoroutine(BlinkFeedbackRoutine());
    }

    private void StopBlinkFeedback()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }

        _hideFeedbackForBlink = false;
    }

    private IEnumerator BlinkFeedbackRoutine()
    {
        for (int i = 0; i < hitBlinkCount; i++)
        {
            _hideFeedbackForBlink = false;
            ApplyVisual();
            yield return new WaitForSeconds(hitBlinkDuration);

            _hideFeedbackForBlink = true;
            ApplyVisual();
            yield return new WaitForSeconds(hitBlinkDuration);
        }

        _hideFeedbackForBlink = false;
        _blinkCoroutine = null;
        ApplyVisual();
    }

    private void ApplyVisual(bool immediate = false)
    {
        if (_tubeMaterial == null) return;

        Color targetColor = normalColor;
        float targetEmission = normalEmission;

        if (!_hideFeedbackForBlink)
        {
            if (_isFailure || _isShowingSequence)
            {
                targetColor = sequenceColor;
                targetEmission = feedbackEmission;
            }
            else if (_isCorrect)
            {
                targetColor = correctColor;
                targetEmission = feedbackEmission;
            }
            else if (_isRoundTransition || _isHovered)
            {
                targetColor = _isRoundTransition ? roundTransitionColor : hoverColor;
                targetEmission = feedbackEmission;
            }
        }

        if (immediate)
        {
            SetVisual(targetColor, targetEmission);
            return;
        }

        if (_colorTransitionCoroutine != null) StopCoroutine(_colorTransitionCoroutine);
        _colorTransitionCoroutine = StartCoroutine(TransitionVisual(targetColor, targetEmission));
    }

    private IEnumerator TransitionVisual(Color targetColor, float targetEmission)
    {
        Color initialColor = _tubeMaterial.HasProperty(ColorProperty)
            ? _tubeMaterial.GetColor(ColorProperty)
            : normalColor;
        float initialEmission = _tubeMaterial.HasProperty(EmissionProperty)
            ? _tubeMaterial.GetFloat(EmissionProperty)
            : normalEmission;
        float elapsedTime = 0f;

        while (elapsedTime < colorTransitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / colorTransitionDuration);
            SetVisual(
                Color.Lerp(initialColor, targetColor, progress),
                Mathf.Lerp(initialEmission, targetEmission, progress)
            );
            yield return null;
        }

        SetVisual(targetColor, targetEmission);
        _colorTransitionCoroutine = null;
    }

    private void EnsureGlowLight()
    {
        if (glowLight != null || !createGlowLightIfMissing) return;

        GameObject glowObject = new GameObject("EngineMinigameGlow");
        glowObject.transform.SetParent(transform, false);
        glowLight = glowObject.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.range = glowLightRange;
        glowLight.intensity = 0f;
        glowLight.shadows = LightShadows.None;
    }

    private void SetVisual(Color baseColor, float emission)
    {
        float glowAmount = Mathf.Clamp01(emission / Mathf.Max(feedbackEmission, 0.01f));
        Color surfaceColor = Color.Lerp(baseColor, baseColor * surfaceGlowMultiplier, glowAmount);

        if (_tubeMaterial.HasProperty(ColorProperty))
            _tubeMaterial.SetColor(ColorProperty, surfaceColor);
        if (_tubeMaterial.HasProperty(EmissionProperty))
            _tubeMaterial.SetFloat(EmissionProperty, emission);
        if (_tubeMaterial.HasProperty(EmissionColorProperty))
        {
            _tubeMaterial.EnableKeyword("_EMISSION");
            _tubeMaterial.SetColor(EmissionColorProperty, baseColor * (emission * materialEmissionIntensity));
        }

        if (glowLight != null)
        {
            glowLight.color = baseColor;
            glowLight.range = glowLightRange;
            glowLight.intensity = glowAmount * glowLightIntensity;
        }
    }
}
