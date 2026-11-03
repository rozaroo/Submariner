using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>Reveals a complete TMP text without changing its layout as letters appear.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AkGameObj))]
[RequireComponent(typeof(CinematicEndButton))]
public class CanvasTypewriter : MonoBehaviour
{
    [Header("Story")]
    [SerializeField] private TMP_Text targetText;
    [Tooltip("Insert <page> between passages. Only these markers create pages; text is never split automatically.")]
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

    [Header("Pages")]
    [Tooltip("Optional object shown while waiting for a click to read the next page.")]
    [SerializeField] private GameObject continuePrompt;
    [Tooltip("Optional text displaying the current page, for example 1/4.")]
    [SerializeField] private TMP_Text pageCounter;

    [Header("Wwise sound")]
    [Tooltip("A short, non-looping key event. Its SoundBank must already be loaded.")]
    [SerializeField] private string typingEvent;
    [Tooltip("Limits overlapping key sounds at high writing speeds.")]
    [Min(0f)] [SerializeField] private float minimumSoundInterval = 0.035f;

    [Header("Events")]
    [Tooltip("Fires after the last page or SkipToEnd. Does not fire between pages or when stopped.")]
    [SerializeField] private UnityEvent onCompleted = new();

    public bool IsTyping { get; private set; }
    public bool IsFastForwarding { get; private set; }
    public bool IsWaitingForNextPage { get; private set; }
    public int CurrentPage => pages.Length == 0 ? 0 : pageIndex + 1;
    public int TotalPages => pages.Length;
    public bool IsFinalPage => TotalPages > 0 && CurrentPage == TotalPages;
    public bool HasCompleted { get; private set; }
    public TMP_Text TextComponent => targetText;
    public event System.Action StoryStarted;
    public event System.Action StoryCompleted;

    private int visibleCharacters;
    private int characterCount;
    private float remainingDelay;
    private float nextSoundTime;
    private uint playingEventId;
    private bool waitingForClick;
    private int clickReadyFrame;
    private int pageIndex;
    private string[] pages = System.Array.Empty<string>();

    private void Awake()
    {
        // Also supports existing scene instances created before AkGameObj was required.
        if (GetComponent<AkGameObj>() == null) gameObject.AddComponent<AkGameObj>();
        // Restore the cinematic exit in older scene instances that lost the companion component.
        if (GetComponent<CinematicEndButton>() == null) gameObject.AddComponent<CinematicEndButton>();
    }

    private void OnEnable()
    {
        SetContinuePrompt(false);
        if (pageCounter != null && pageCounter != targetText) pageCounter.text = string.Empty;
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

        StoryStarted?.Invoke();

        // Render one author-defined passage at a time. TMP must not create extra pages.
        targetText.enableAutoSizing = false;
        targetText.textWrappingMode = TextWrappingModes.Normal;
        targetText.overflowMode = TextOverflowModes.Overflow;
        targetText.richText = true;
        targetText.firstVisibleCharacter = 0;
        targetText.useMaxVisibleDescender = false;
        targetText.pageToDisplay = 1;
        pageIndex = 0;
        pages = System.Array.FindAll(
            System.Text.RegularExpressions.Regex.Split(text ?? string.Empty, @"<page\s*>",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            passage => !string.IsNullOrWhiteSpace(passage));
        if (TotalPages == 0)
        {
            targetText.text = string.Empty;
            targetText.maxVisibleCharacters = 0;
            UpdatePageCounter();
            Complete();
            return;
        }
        BeginPage(0);
    }

    private void BeginPage(int index)
    {
        HasCompleted = false;
        pageIndex = index;
        targetText.text = pages[pageIndex].Trim();
        targetText.maxVisibleCharacters = 0;
        Canvas.ForceUpdateCanvases();
        targetText.ForceMeshUpdate(true);
        characterCount = targetText.textInfo.characterCount;
        visibleCharacters = 0;
        IsWaitingForNextPage = false;
        IsFastForwarding = false;
        SetContinuePrompt(false);
        UpdatePageCounter();
        remainingDelay = Mathf.Max(0f, initialDelay);
        nextSoundTime = float.NegativeInfinity;
        IsTyping = true;
    }

    private void Update()
    {
        // Ignore the click that may have opened/enabled this cinematic itself.
        if (Time.frameCount > clickReadyFrame
            && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (waitingForClick) Play();
            else if (IsTyping) ToggleSpeed();
            else if (IsWaitingForNextPage) NextPage();
        }
        if (!IsTyping) return;
        if (targetText == null) { Stop(); return; }
        float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (delta <= 0f) return;
        remainingDelay -= delta * (IsFastForwarding ? Mathf.Max(1f, fastForwardMultiplier) : 1f);
        // Catch up at low frame rates instead of limiting writing to one character per frame.
        while (IsTyping && remainingDelay <= 0f)
        {
            if (visibleCharacters >= characterCount) { FinishPage(); return; }
            var character = targetText.textInfo.characterInfo[visibleCharacters];
            targetText.maxVisibleCharacters = ++visibleCharacters;
            if (!char.IsWhiteSpace(character.character)) PlayKeySound();
            remainingDelay += CharacterDelay(character.character);
        }
    }

    private void FinishPage()
    {
        IsTyping = false;
        IsFastForwarding = false;
        if (pageIndex + 1 >= TotalPages) { Complete(); return; }
        IsWaitingForNextPage = true;
        clickReadyFrame = Time.frameCount;
        SetContinuePrompt(true);
    }

    /// <summary>Advance only after the current page has finished writing.</summary>
    public void NextPage()
    {
        if (!IsWaitingForNextPage || !isActiveAndEnabled) return;
        if (targetText == null) { Stop(); return; }
        BeginPage(pageIndex + 1);
    }

    private void SetContinuePrompt(bool visible)
    {
        // The prompt must be a separate child, not the object running this component.
        if (continuePrompt != null && !transform.IsChildOf(continuePrompt.transform))
            continuePrompt.SetActive(visible);
    }

    private void UpdatePageCounter()
    {
        if (pageCounter != null && pageCounter != targetText)
            pageCounter.text = CurrentPage + "/" + TotalPages;
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

    /// <summary>Jump to the final page and signal completion without the remaining key sounds.</summary>
    [ContextMenu("Skip to end")]
    public void SkipToEnd()
    {
        if (!IsTyping && !IsWaitingForNextPage) return;
        StopSound();
        if (targetText != null)
        {
            BeginPage(TotalPages - 1);
            targetText.maxVisibleCharacters = int.MaxValue;
        }
        Complete();
    }

    /// <summary>Stop writing, leaving the currently visible text on screen.</summary>
    public void Stop()
    {
        HasCompleted = false;
        waitingForClick = false;
        IsWaitingForNextPage = false;
        IsTyping = false;
        IsFastForwarding = false;
        SetContinuePrompt(false);
        StopSound();
    }

    private void StopSound()
    {
        if (playingEventId != 0) AkSoundEngine.StopPlayingID(playingEventId);
        playingEventId = 0;
    }

    private void Complete()
    {
        // Completion is a story-level event, never an intermediate-page event.
        if (HasCompleted || (TotalPages > 0 && !IsFinalPage)) return;
        HasCompleted = true;
        IsTyping = false;
        IsFastForwarding = false;
        IsWaitingForNextPage = false;
        SetContinuePrompt(false);
        StoryCompleted?.Invoke();
        onCompleted.Invoke();
    }

    private void OnDisable() => Stop();
}
