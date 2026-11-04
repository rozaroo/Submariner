using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Control the cinematic's exit buttons and scene fade.</summary>
[DisallowMultipleComponent]
public class CinematicEndButton : MonoBehaviour
{
    [Header("Connection")]
    [SerializeField] private CanvasTypewriter typewriter;
    [Tooltip("Optional custom button. If empty, a button is created in the story's Canvas.")]
    [SerializeField] private Button immersionButton;
    [Tooltip("Optional scene button that skips the cinematic.")]
    [SerializeField] private Button skipButton;
    [SerializeField] private string buttonLabel = "Inmersión";
    [SerializeField] private string targetScene = "InteriorSubmarine";

    [Header("Appearance timing")]
    [Min(0f)] [SerializeField] private float revealDelay = 0.4f;
    [Min(0f)] [SerializeField] private float buttonFadeInDuration = 1.8f;

    [Header("Generated button placement")]
    [SerializeField] private Vector2 buttonAnchor = new(0.5f, 0f);
    [SerializeField] private Vector2 buttonPosition = new(0f, 80f);
    [SerializeField] private Vector2 buttonSize = new(280f, 64f);
    [Min(1f)] [SerializeField] private float labelFontSize = 28f;

    private CanvasGroup buttonGroup;
    private bool revealing;
    private bool transitioning;
    private bool ownsButton;
    private bool CanShowButton => typewriter != null && typewriter.HasCompleted
        && typewriter.IsFinalPage && !typewriter.IsTyping && !typewriter.IsWaitingForNextPage;

    private void Awake()
    {
        if (typewriter == null) typewriter = GetComponent<CanvasTypewriter>();
        if (typewriter == null)
        {
            Debug.LogWarning("[CinematicEndButton] Assign the Canvas Typewriter.", this);
            enabled = false;
            return;
        }
        if (immersionButton == null)
        {
            immersionButton = CreateImmersionButton();
            ownsButton = immersionButton != null;
        }
        if (immersionButton == null) { enabled = false; return; }
        buttonGroup = immersionButton.GetComponent<CanvasGroup>();
        if (buttonGroup == null) buttonGroup = immersionButton.gameObject.AddComponent<CanvasGroup>();
        TMP_Text label = immersionButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = buttonLabel;
        ResetButton();
    }

    private void OnEnable()
    {
        if (typewriter == null || immersionButton == null) return;
        typewriter.StoryCompleted += ShowButton;
        typewriter.StoryStarted += ResetButton;
        immersionButton.onClick.AddListener(BeginImmersion);
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(BeginSkip);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (typewriter.HasCompleted) ShowButton();
    }

    private Button CreateImmersionButton()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        TMP_Text storyText = typewriter.TextComponent;
        if (canvas == null && storyText != null) canvas = storyText.canvas;
        if (canvas == null)
        {
            Debug.LogWarning("[CinematicEndButton] A Canvas or a custom button is required.", this);
            return null;
        }
        var buttonObject = new GameObject(buttonLabel, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.layer = canvas.gameObject.layer;
        buttonObject.transform.SetParent(canvas.transform, false);
        var rect = (RectTransform)buttonObject.transform;
        rect.anchorMin = rect.anchorMax = buttonAnchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = buttonPosition;
        rect.sizeDelta = buttonSize;
        var background = buttonObject.GetComponent<Image>();
        background.color = new Color(0.12f, 0.16f, 0.16f, 0.95f);
        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;
        var colors = button.colors;
        colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        button.colors = colors;

        var textObject = new GameObject("Label", typeof(RectTransform));
        textObject.layer = buttonObject.layer;
        textObject.transform.SetParent(buttonObject.transform, false);
        var label = textObject.AddComponent<TextMeshProUGUI>();
        if (storyText != null) label.font = storyText.font;
        label.text = buttonLabel;
        label.fontSize = labelFontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.9f, 0.94f, 0.9f);
        label.raycastTarget = false;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(12f, 4f);
        label.rectTransform.offsetMax = new Vector2(-12f, -4f);
        return button;
    }

    internal bool IsSkipButtonAt(Vector2 screenPosition)
    {
        if (!isActiveAndEnabled || skipButton == null || !skipButton.isActiveAndEnabled
            || !skipButton.IsInteractable() || skipButton.transform is not RectTransform rect) return false;
        Canvas canvas = skipButton.GetComponentInParent<Canvas>();
        if (canvas == null) return false;
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, eventCamera);
    }

    private void ResetButton()
    {
        StopAllCoroutines();
        revealing = false;
        if (buttonGroup == null) return;
        buttonGroup.alpha = 0f;
        buttonGroup.interactable = false;
        buttonGroup.blocksRaycasts = false;
        immersionButton.interactable = false;
    }

    public void ShowButton()
    {
        // Check here as well as at the event source: Inspector callbacks can call this directly.
        if (!CanShowButton || !isActiveAndEnabled || buttonGroup == null || revealing || transitioning) return;
        revealing = true;
        immersionButton.gameObject.SetActive(true);
        StartCoroutine(RevealButton());
    }

    private void LateUpdate()
    {
        // Hide immediately if playback restarts or moves away from the completed final page.
        if (!transitioning && buttonGroup != null && !CanShowButton && (revealing || buttonGroup.alpha > 0f))
            ResetButton();
    }

    private IEnumerator RevealButton()
    {
        float timer = 0f;
        while (timer < revealDelay)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
        timer = 0f;
        while (timer < buttonFadeInDuration)
        {
            timer += Time.unscaledDeltaTime;
            buttonGroup.alpha = Mathf.SmoothStep(0f, 1f, timer / buttonFadeInDuration);
            yield return null;
        }
        buttonGroup.alpha = 1f;
        // The completion click must not accidentally activate a newly revealed button.
        yield return null;
        buttonGroup.interactable = true;
        buttonGroup.blocksRaycasts = true;
        immersionButton.interactable = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void BeginImmersion()
    {
        if (!CanShowButton || immersionButton == null || !immersionButton.interactable) return;
        BeginTransition();
    }

    private void BeginSkip() => BeginTransition();

    private void BeginTransition()
    {
        if (transitioning) return;
        if (!Application.CanStreamedLevelBeLoaded(targetScene))
        {
            Debug.LogError("[CinematicEndButton] Add the target scene to the active build profile: " + targetScene, this);
            return;
        }
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError("[CinematicEndButton] SceneTransitionManager is unavailable.", this);
            return;
        }
        transitioning = true;
        StopAllCoroutines();
        if (!typewriter.HasCompleted) typewriter.Stop();
        immersionButton.interactable = false;
        buttonGroup.interactable = false;
        if (skipButton != null) skipButton.interactable = false;
        SceneTransitionManager.Instance.LoadSceneWithFade(targetScene);
    }

    private void OnDisable()
    {
        if (typewriter != null)
        {
            typewriter.StoryCompleted -= ShowButton;
            typewriter.StoryStarted -= ResetButton;
        }
        if (immersionButton != null) immersionButton.onClick.RemoveListener(BeginImmersion);
        if (skipButton != null) skipButton.onClick.RemoveListener(BeginSkip);
        transitioning = false;
        ResetButton();
    }

    private void OnDestroy()
    {
        if (ownsButton && immersionButton != null) Destroy(immersionButton.gameObject);
    }
}
