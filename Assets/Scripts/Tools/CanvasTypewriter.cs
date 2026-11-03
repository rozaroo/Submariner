using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>Reveals a complete TMP text without changing its layout as letters appear.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AkGameObj))]
public class CanvasTypewriter : MonoBehaviour
{
    [Header("Story")]
    [SerializeField] private TMP_Text targetText;
    [TextArea(5, 20)] [SerializeField] private string storyText;
    [Tooltip("Wait for the first left mouse click anywhere on screen before writing.")]
    [SerializeField] private bool startOnClick = true;
    [Tooltip("Used only when Start On Click is disabled.")]
    [SerializeField] private bool playOnEnable;

    [Header("Writing")]
    [Min(1f)] [SerializeField] private float charactersPerSecond = 35f;
    [Tooltip("Each click while writing toggles this speed multiplier, including punctuation pauses.")]
    [Min(1f)] [SerializeField] private float fastForwardMultiplier = 4f;
    [Min(0f)] [SerializeField] private float initialDelay = 0.3f;
    [Min(0f)] [SerializeField] private float commaPause = 0.12f;
    [Min(0f)] [SerializeField] private float sentencePause = 0.3f;
    [Min(0f)] [SerializeField] private float lineBreakPause = 0.2f;
    [Tooltip("Continue writing even when Time.timeScale is zero.")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Wwise sound")]
    [Tooltip("A short, non-looping key event. Its SoundBank must already be loaded.")]
    [SerializeField] private string typingEvent;
    [Tooltip("Limits overlapping key sounds at high writing speeds.")]
    [Min(0f)] [SerializeField] private float minimumSoundInterval = 0.035f;

    [Header("Events")]
    [Tooltip("Also fires when SkipToEnd is called. Does not fire when stopped or disabled.")]
    [SerializeField] private UnityEvent onCompleted = new();

    public bool IsTyping { get; private set; }
    public bool IsFastForwarding { get; private set; }

    private int visibleCharacters;
    private int characterCount;
    private float remainingDelay;
    private float nextSoundTime;
    private uint playingEventId;
    private bool waitingForClick;
    private int clickReadyFrame;

    private void Awake()
    {
        // Also supports existing scene instances created before AkGameObj was required.
        if (GetComponent<AkGameObj>() == null) gameObject.AddComponent<AkGameObj>();
    }

    private void OnEnable()
    {
        waitingForClick = startOnClick;
        clickReadyFrame = Time.frameCount;
        if (waitingForClick)
        {
            if (targetText != null) targetText.maxVisibleCharacters = 0;
            return;
        }
        if (playOnEnable) Play();
    }

    /// <summary>Start or restart the story configured in the Inspector.</summary>
    [ContextMenu("Play story")]
    public void Play() => Play(storyText);

    /// <summary>Start a different passage, for example from a future cinematic controller.</summary>
    public void Play(string text)
    {
        Stop();
        if (!isActiveAndEnabled) return;
        if (targetText == null)
        {
            Debug.LogWarning("[CanvasTypewriter] Assign Target Text in the Inspector.", this);
            return;
        }

        targetText.text = text ?? string.Empty;
        targetText.maxVisibleCharacters = 0;
        targetText.ForceMeshUpdate(true);
        characterCount = targetText.textInfo.characterCount;
        visibleCharacters = 0;
        remainingDelay = Mathf.Max(0f, initialDelay);
        nextSoundTime = float.NegativeInfinity;
        IsTyping = true;
        if (characterCount == 0) Complete();
    }

    private void Update()
    {
        // Ignore the click that may have opened/enabled this cinematic itself.
        if (Time.frameCount > clickReadyFrame
            && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (waitingForClick) Play();
            else if (IsTyping) ToggleSpeed();
        }
        if (!IsTyping) return;
        if (targetText == null) { Stop(); return; }
        float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (delta <= 0f) return;
        remainingDelay -= delta * (IsFastForwarding ? Mathf.Max(1f, fastForwardMultiplier) : 1f);
        // Catch up at low frame rates instead of limiting writing to one character per frame.
        while (IsTyping && remainingDelay <= 0f)
        {
            if (visibleCharacters >= characterCount) { Complete(); return; }
            var character = targetText.textInfo.characterInfo[visibleCharacters];
            targetText.maxVisibleCharacters = ++visibleCharacters;
            if (!char.IsWhiteSpace(character.character)) PlayKeySound();
            remainingDelay += CharacterDelay(character.character);
        }
    }

    /// <summary>Alternate fast and normal writing without restarting or skipping the story.</summary>
    public void ToggleSpeed()
    {
        if (IsTyping) IsFastForwarding = !IsFastForwarding;
    }

    private float CharacterDelay(char character)
    {
        float interval = 1f / Mathf.Max(1f, charactersPerSecond);
        switch (character)
        {
            case ',': case ';': case ':': return interval + Mathf.Max(0f, commaPause);
            case '.': case '!': case '?': case '\u2026': return interval + Mathf.Max(0f, sentencePause);
            case '\n': return interval + Mathf.Max(0f, lineBreakPause);
            default: return interval;
        }
    }

    private void PlayKeySound()
    {
        float now = useUnscaledTime ? Time.unscaledTime : Time.time;
        if (now < nextSoundTime) return;
        nextSoundTime = now + Mathf.Max(0f, minimumSoundInterval);
        // Keep ownership of a single key event so stopping never affects other game audio.
        SFXManager.PostEvent(typingEvent, gameObject);
    }

    /// <summary>Reveal all text and signal completion without playing the remaining key sounds.</summary>
    [ContextMenu("Skip to end")]
    public void SkipToEnd()
    {
        if (!IsTyping) return;
        StopSound();
        if (targetText != null) targetText.maxVisibleCharacters = int.MaxValue;
        Complete();
    }

    /// <summary>Stop writing, leaving the currently visible text on screen.</summary>
    public void Stop()
    {
        waitingForClick = false;
        IsTyping = false;
        IsFastForwarding = false;
        StopSound();
    }

    private void StopSound()
    {
        if (playingEventId != 0) AkSoundEngine.StopPlayingID(playingEventId);
        playingEventId = 0;
    }

    private void Complete()
    {
        IsTyping = false;
        IsFastForwarding = false;
        onCompleted.Invoke();
    }

    private void OnDisable() => Stop();
}
