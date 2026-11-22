using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    private const float FinalBlinkDuration = 2.4f;
    private const float FinalBlinkHoldDuration = 0.35f;
    private const float BlackFadeStart = 0.83f;
    private static readonly AnimationCurve FinalBlinkCurve = new(
        new Keyframe(0f, 0f),
        new Keyframe(0.34f, 0.28f),
        new Keyframe(0.52f, 0.21f),
        new Keyframe(1f, 1f));

    public static SceneTransitionManager Instance { get; private set; }

    private Image fadeImage;
    private Coroutine fadeRoutine;
    private float fadeOutDuration = 1.5f;
    private float fadeInDuration = 4.0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Instance == null)
        {
            GameObject managerObj = new GameObject("SceneTransitionManager");
            Instance = managerObj.AddComponent<SceneTransitionManager>();
            DontDestroyOnLoad(managerObj);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        CreateFadeUI();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void CreateFadeUI()
    {
        GameObject canvasObj = new GameObject("FadeCanvas");
        canvasObj.transform.SetParent(transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform, false);
        fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0);
        
        RectTransform rect = fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        
        fadeImage.raycastTarget = true; 
        fadeImage.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartFade(FadeIn());
    }

    public void LoadSceneWithFade(string sceneName)
    {
        StartFade(FadeOutAndLoad(sceneName));
    }

    public void LoadSceneAfterSuffocation(string sceneName, SuffocationEffect suffocationEffect)
    {
        StartFade(FadeOutAndLoad(sceneName, suffocationEffect));
    }

    private void StartFade(IEnumerator fade)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(fade);
    }

    private IEnumerator FadeOutAndLoad(string sceneName, SuffocationEffect suffocationEffect = null)
    {
        Time.timeScale = 0f;
        fadeImage.gameObject.SetActive(true);
        fadeImage.raycastTarget = true;
        bool useFinalBlink = suffocationEffect != null && suffocationEffect.TryBeginFinalBlink();

        float timer = 0f;
        float duration = useFinalBlink ? FinalBlinkDuration : fadeOutDuration;
        Color color = fadeImage.color;
        float startAlpha = color.a;
        
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = timer / duration;
            if (useFinalBlink && suffocationEffect != null)
                suffocationEffect.SetFinalBlinkProgress(FinalBlinkCurve.Evaluate(Mathf.Clamp01(progress)));

            // Let the camera's blink effect play first, then cover the last bit of light for the scene load.
            float blackProgress = useFinalBlink ? Mathf.InverseLerp(BlackFadeStart, 1f, progress) : progress;
            color.a = Mathf.Lerp(startAlpha, 1f, blackProgress);
            fadeImage.color = color;
            yield return null;
        }
        
        color.a = 1f;
        fadeImage.color = color;
        if (useFinalBlink) yield return new WaitForSecondsRealtime(FinalBlinkHoldDuration);

        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeIn()
    {
        fadeImage.gameObject.SetActive(true);
        fadeImage.raycastTarget = true;

        float timer = 0f;
        Color color = fadeImage.color;
        color.a = 1f;
        fadeImage.color = color;

        Time.timeScale = 0f; 

        while (timer < fadeInDuration)
        {
            timer += Time.unscaledDeltaTime;
            color.a = Mathf.Clamp01(1f - (timer / fadeInDuration));
            fadeImage.color = color;
            yield return null;
        }

        color.a = 0f;
        fadeImage.color = color;
        fadeImage.gameObject.SetActive(false);
        fadeImage.raycastTarget = false;

        Time.timeScale = 1f;
        fadeRoutine = null;
    }
}
