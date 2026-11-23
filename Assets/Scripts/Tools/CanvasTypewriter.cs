using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Reveals a complete TMP text without changing its layout as letters appear.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AkGameObj))]
[RequireComponent(typeof(CinematicEndButton))]
public class CanvasTypewriter : MonoBehaviour
{
    private static readonly Regex WarningTag = new(@"<warning>(.+?)</warning>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private const string WarningColorTag = "<color=#CF101C>";

    [Header("Story")]
    [SerializeField] private TMP_Text targetText;
    [Tooltip("Insert <page> between passages. Only these markers create pages; text is never split automatically.")]
    [TextArea(5, 20)] [SerializeField] private string storyText;
    [Tooltip("Wait for the first left mouse click anywhere on screen before writing.")]
    [SerializeField] private bool startOnClick = true;
    [Tooltip("Used only when Start On Click is disabled.")]
    [SerializeField] private bool playOnEnable;
    [Tooltip("Minimum reading time after revealing a page with <warning>...</warning>.")]
    [Min(0.1f)] [SerializeField] private float warningReadSeconds = 5f;

    [Header("Writing")]
    [Min(1f)] [SerializeField] private float charactersPerSecond = 35f;
    [Tooltip("Writing multiplier for the highest speed setting (x3).")]
    [Min(3f)] [SerializeField] private float thirdSpeedMultiplier = 3f;
    [SerializeField] private Button speedButton;
    [SerializeField] private TMP_Text speedLabel;
    [Min(0f)] [SerializeField] private float initialDelay = 0.3f;
    [Min(0f)] [SerializeField] private float commaPause = 0.12f;
    [Min(0f)] [SerializeField] private float sentencePause = 0.3f;
    [Min(0f)] [SerializeField] private float lineBreakPause = 0.2f;
    [Tooltip("Continue writing even when Time.timeScale is zero.")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Pages")]
    [SerializeField] private Button revealPageButton;
    [Tooltip("Optional object shown before the first click and between pages. Add a Canvas Group to pulse its opacity.")]
    [SerializeField] private GameObject continuePrompt;
    [Min(0.2f)] [SerializeField] private float promptBlinkPeriod = 1.4f;
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
    public bool IsFastForwarding => IsTyping && speedLevel > 1;
    public bool IsWaitingForNextPage { get; private set; }
    public int CurrentPage => pages.Length == 0 ? 0 : pageIndex + 1;
    public int TotalPages => pages.Length;
    public bool IsFinalPage => TotalPages > 0 && CurrentPage == TotalPages;
    public bool HasCompleted { get; private set; }
    internal bool IsWarningLocked => warningPageIndex >= 0 && pageIndex == warningPageIndex && !warningRead;
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
    private int speedLevel = 1;
    private CanvasGroup continuePromptGroup;
    private float promptShownAt;
    private CinematicEndButton endButton;
    private int pageIndex;
    private string[] pages = System.Array.Empty<string>();
    private int warningPageIndex = -1;
    private bool warningRead;
    private float warningUnlockAt;
    private int warningTextStart;
    private int warningTextEnd;

    private void Awake()
    {
        // Also supports existing scene instances created before AkGameObj was required.
        if (GetComponent<AkGameObj>() == null) gameObject.AddComponent<AkGameObj>();
        // Restore the cinematic exit in older scene instances that lost the companion component.
        endButton = GetComponent<CinematicEndButton>();
        if (endButton == null) endButton = gameObject.AddComponent<CinematicEndButton>();
        if (continuePrompt != null) continuePromptGroup = continuePrompt.GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (targetText != null) targetText.OnPreRenderText += ShakeWarning;
        speedLevel = 1;
        UpdateSpeedLabel();
        if (speedButton != null) speedButton.onClick.AddListener(ToggleSpeed);
        if (revealPageButton != null)
        {
            revealPageButton.onClick.AddListener(RevealCurrentPage);
            revealPageButton.interactable = false;
        }
        if (pageCounter != null && pageCounter != targetText) pageCounter.text = string.Empty;
        waitingForClick = startOnClick;
        clickReadyFrame = Time.frameCount;
        SetContinuePrompt(waitingForClick);
        SetStoryControlsVisible(waitingForClick);
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
            Regex.Split(text ?? string.Empty, @"<page\s*>", RegexOptions.IgnoreCase),
            passage => !string.IsNullOrWhiteSpace(passage));
        warningPageIndex = System.Array.FindIndex(pages, passage => WarningTag.IsMatch(passage));
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
        warningUnlockAt = float.PositiveInfinity;
        warningTextStart = warningTextEnd = 0;
        string passage = pages[pageIndex].Trim();
        Match warning = WarningTag.Match(passage);
        if (warning.Success)
        {
            warningTextStart = warning.Index + WarningColorTag.Length;
            warningTextEnd = warningTextStart + warning.Groups[1].Length;
            passage = WarningTag.Replace(passage, WarningColorTag + "$1</color>", 1);
        }

        targetText.text = passage;
        targetText.maxVisibleCharacters = 0;
        Canvas.ForceUpdateCanvases();
        targetText.ForceMeshUpdate(true);
        characterCount = targetText.textInfo.characterCount;
        visibleCharacters = 0;
        IsWaitingForNextPage = false;
        SetContinuePrompt(false);
        SetStoryControlsVisible(true);
        if (revealPageButton != null) revealPageButton.interactable = true;
        UpdatePageCounter();
        remainingDelay = Mathf.Max(0f, initialDelay);
        nextSoundTime = float.NegativeInfinity;
        IsTyping = true;
    }

    private void Update()
    {
        // Ignore the click that may have opened/enabled this cinematic itself.
        Mouse mouse = Mouse.current;
        if (Time.frameCount > clickReadyFrame && mouse != null)
        {
            if (IsTyping && mouse.rightButton.wasPressedThisFrame)
            {
                RevealCurrentPage();
            }
            else if (mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 pointerPosition = mouse.position.ReadValue();
                if (!CinematicEndButton.IsButtonAt(speedButton, pointerPosition)
                    && !CinematicEndButton.IsButtonAt(revealPageButton, pointerPosition, requireInteractable: false)
                    && (endButton == null || !endButton.IsSkipButtonAt(pointerPosition)))
                {
                    if (waitingForClick) Play();
                    else if (IsTyping) ToggleSpeed();
                    else if (IsWaitingForNextPage) NextPage();
                }
            }
        }
        if (waitingForClick || IsWaitingForNextPage) UpdateContinuePrompt();
        if (!IsTyping) return;
        if (targetText == null) { Stop(); return; }
        float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (delta <= 0f) return;
        float multiplier = speedLevel == 3 ? Mathf.Max(3f, thirdSpeedMultiplier) : speedLevel;
        remainingDelay -= delta * multiplier;
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
        if (revealPageButton != null) revealPageButton.interactable = false;
        if (IsWarningLocked)
        {
            warningUnlockAt = Time.unscaledTime + Mathf.Max(0.1f, warningReadSeconds);
            return;
        }
        AllowNextPage();
    }

    private void AllowNextPage()
    {
        if (pageIndex + 1 >= TotalPages) { Complete(); return; }
        IsWaitingForNextPage = true;
        clickReadyFrame = Time.frameCount;
        SetContinuePrompt(true);
    }

    private void LateUpdate()
    {
        if (!IsWarningLocked || targetText == null) return;

        if (Time.unscaledTime >= warningUnlockAt)
        {
            warningRead = true;
            targetText.ForceMeshUpdate();
            AllowNextPage();
        }
        else if (visibleCharacters > 0)
        {
            targetText.ForceMeshUpdate();
        }
    }

    private void ShakeWarning(TMP_TextInfo textInfo)
    {
        if (!IsWarningLocked || warningTextStart == warningTextEnd) return;

        // Move the phrase as a group so the letters stay readable.
        float step = Mathf.Floor(Time.unscaledTime * 24f);
        float amplitude = targetText.fontSize * 0.065f;
        Vector3 offset = new Vector3(Mathf.Sin(step * 2.4f), Mathf.Cos(step * 1.62f) * 0.55f) * amplitude;
        int count = Mathf.Min(visibleCharacters, textInfo.characterCount);
        for (int i = 0; i < count; i++)
        {
            TMP_CharacterInfo character = textInfo.characterInfo[i];
            if (character.index >= warningTextEnd) break;
            if (character.index < warningTextStart || !character.isVisible) continue;

            Vector3[] vertices = textInfo.meshInfo[character.materialReferenceIndex].vertices;
            for (int corner = 0; corner < 4; corner++)
                vertices[character.vertexIndex + corner] += offset;
        }
    }

    private void RevealCurrentPage()
    {
        if (!isActiveAndEnabled || !IsTyping || targetText == null) return;
        visibleCharacters = characterCount;
        targetText.maxVisibleCharacters = visibleCharacters;
        FinishPage();
    }

    /// <summary>Advance only after the current page has finished writing.</summary>
    public void NextPage()
    {
        if (!IsWaitingForNextPage || IsWarningLocked || !isActiveAndEnabled) return;
        if (targetText == null) { Stop(); return; }
        BeginPage(pageIndex + 1);
    }

    private void SetContinuePrompt(bool visible)
    {
        // The prompt must be a separate child, not the object running this component.
        if (continuePrompt == null || transform.IsChildOf(continuePrompt.transform)) return;
        continuePrompt.SetActive(visible);
        if (!visible || continuePromptGroup == null) return;
        promptShownAt = Time.unscaledTime;
        continuePromptGroup.alpha = 1f;
    }

    private void UpdateContinuePrompt()
    {
        if (continuePromptGroup == null || !continuePromptGroup.isActiveAndEnabled) return;
        float phase = (Time.unscaledTime - promptShownAt) / Mathf.Max(0.2f, promptBlinkPeriod);
        continuePromptGroup.alpha = Mathf.Clamp01(0.35f + 0.65f * Mathf.Cos(phase * 2f * Mathf.PI));
    }

    private void UpdatePageCounter()
    {
        if (pageCounter != null && pageCounter != targetText)
            pageCounter.text = CurrentPage + "/" + TotalPages;
    }

    /// <summary>Cycle through the writing speeds without restarting or advancing the story.</summary>
    public void ToggleSpeed()
    {
        if (!isActiveAndEnabled || (!waitingForClick && !IsTyping && !IsWaitingForNextPage)) return;
        speedLevel = speedLevel == 3 ? 1 : speedLevel + 1;
        UpdateSpeedLabel();
    }

    private void UpdateSpeedLabel()
    {
        if (speedLabel != null) speedLabel.text = $"x{speedLevel}";
    }

    private void SetStoryControlsVisible(bool visible)
    {
        if (speedButton != null) speedButton.gameObject.SetActive(visible);
        if (revealPageButton != null) revealPageButton.gameObject.SetActive(visible);
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
        if ((!IsTyping && !IsWaitingForNextPage) || !TrySkipPastWarning()) return;
        StopSound();
        if (targetText != null)
        {
            BeginPage(TotalPages - 1);
            targetText.maxVisibleCharacters = int.MaxValue;
        }
        Complete();
    }

    internal bool TrySkipPastWarning()
    {
        if (waitingForClick && WarningTag.IsMatch(storyText ?? string.Empty)) Play();
        if (warningPageIndex < 0 || warningRead) return true;
        if (pageIndex != warningPageIndex)
        {
            BeginPage(warningPageIndex);
            RevealCurrentPage();
        }
        return false;
    }

    /// <summary>Stop writing, leaving the currently visible text on screen.</summary>
    public void Stop()
    {
        HasCompleted = false;
        waitingForClick = false;
        IsWaitingForNextPage = false;
        IsTyping = false;
        warningPageIndex = -1;
        warningRead = false;
        SetContinuePrompt(false);
        SetStoryControlsVisible(false);
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
        IsWaitingForNextPage = false;
        SetContinuePrompt(false);
        SetStoryControlsVisible(false);
        StoryCompleted?.Invoke();
        onCompleted.Invoke();
    }

    private void OnDisable()
    {
        if (targetText != null) targetText.OnPreRenderText -= ShakeWarning;
        if (speedButton != null) speedButton.onClick.RemoveListener(ToggleSpeed);
        if (revealPageButton != null) revealPageButton.onClick.RemoveListener(RevealCurrentPage);
        Stop();
    }
}
