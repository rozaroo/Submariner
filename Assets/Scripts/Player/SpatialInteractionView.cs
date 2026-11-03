using TMPro;
using UnityEngine;

/// <summary>World-space renderers only; no screen-space canvas or UI raycast targets.</summary>
internal sealed class SpatialInteractionView
{
    private readonly GameObject root;
    private readonly SpriteRenderer icon;
    private readonly SpriteRenderer discoveryIcon;
    private readonly TextMeshPro label;
    private string layoutText;
    private float layoutTextSize = -1f;
    private float layoutIconWidth = -1f;
    private float layoutIconSize = -1f;
    private Vector3 closeIconPosition;
    private Vector3 closeTextPosition;
    private bool transitionInitialized;
    private bool targetClose;
    private float transitionBlend;
    private float transitionDuration;
    private float farSize;
    private float nearSize;
    private Color tint;
    private readonly float levitationPhase;
    private static Sprite circle;
    public int LastShownFrame { get; private set; } = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCircle() => circle = null;

    public SpatialInteractionView(Transform parent, TMP_FontAsset font)
    {
        root = new GameObject("Spatial Interaction Icon");
        levitationPhase = (root.GetInstanceID() & 1023) * (Mathf.PI * 2f / 1024f);
        root.transform.SetParent(parent, false);
        icon = new GameObject("Key or discovery circle").AddComponent<SpriteRenderer>();
        icon.transform.SetParent(root.transform, false);
        icon.sortingOrder = 1;
        discoveryIcon = new GameObject("Discovery crossfade").AddComponent<SpriteRenderer>();
        discoveryIcon.transform.SetParent(root.transform, false);
        label = new GameObject("Interaction text").AddComponent<TextMeshPro>();
        label.transform.SetParent(root.transform, false);
        label.font = font != null ? font : TMP_Settings.defaultFontAsset;
        label.fontSize = 2;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.rectTransform.sizeDelta = new Vector2(2.5f, 0.6f);
        label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        root.SetActive(false);
    }

    public void Show(Vector3 position, Camera camera, InteractionFeedback feedback, bool close,
        bool focused, Sprite discoverySprite, Sprite interactionSprite, float discoverySize,
        float iconSize, float textSize, Color normalColor, Color focusColor, float duration)
    {
        LastShownFrame = Time.frameCount;
        root.SetActive(true);
        root.transform.position = position;
        // TMP faces local -Z. Keeping camera rotation makes text readable from every angle.
        root.transform.rotation = camera.transform.rotation;
        discoveryIcon.sprite = discoverySprite != null ? discoverySprite : GetCircle();
        Sprite feedbackSprite = feedback.interactionUIData != null ? feedback.interactionUIData.icon : null;
        Sprite nearSprite = interactionSprite != null ? interactionSprite : feedbackSprite;
        icon.sprite = feedback.canInteract && nearSprite != null ? nearSprite : discoveryIcon.sprite;
        targetClose = close;
        transitionDuration = Mathf.Max(0f, duration);
        farSize = discoverySize;
        nearSize = iconSize;
        tint = focused ? focusColor : normalColor;
        if (!transitionInitialized)
        {
            transitionBlend = close ? 1f : 0f;
            transitionInitialized = true;
        }
        Vector2 bounds = icon.sprite.bounds.size;
        if (label.text != feedback.messageToDisplay) label.text = feedback.messageToDisplay;
        label.fontSize = textSize;
        float iconWidth = bounds.x * iconSize / Mathf.Max(0.0001f, Mathf.Max(bounds.x, bounds.y));
        if (layoutText != label.text || layoutTextSize != textSize
            || layoutIconWidth != iconWidth || layoutIconSize != iconSize)
        {
            const float maxTextWidth = 2.5f;
            Vector2 preferred = label.GetPreferredValues(label.text, maxTextWidth, Mathf.Infinity);
            float textWidth = Mathf.Clamp(preferred.x + 0.01f, 0.01f, maxTextWidth);
            label.rectTransform.sizeDelta = new Vector2(textWidth, Mathf.Max(0.01f, preferred.y + 0.01f));
            float gap = iconSize * 0.2f;
            float totalWidth = iconWidth + gap + textWidth;
            closeIconPosition = new Vector3(-totalWidth * 0.5f + iconWidth * 0.5f, 0f, 0f);
            closeTextPosition = new Vector3(totalWidth * 0.5f - textWidth * 0.5f, 0f, 0f);
            layoutText = label.text;
            layoutTextSize = textSize;
            layoutIconWidth = iconWidth;
            layoutIconSize = iconSize;
        }
        ApplyTransition();
    }

    private void ApplyTransition()
    {
        if (transitionDuration <= 0f) transitionBlend = targetClose ? 1f : 0f;
        float blend = Mathf.SmoothStep(0f, 1f, transitionBlend);
        float size = Mathf.Lerp(farSize, nearSize, blend);
        Vector3 position = closeIconPosition * blend;
        bool sameSprite = icon.sprite == discoveryIcon.sprite;
        SetSpritePose(discoveryIcon, size, position, tint, sameSprite ? 1f : 1f - blend);
        SetSpritePose(icon, size, position, tint, sameSprite ? 0f : blend);
        label.gameObject.SetActive(blend > 0f);
        label.color = new Color(tint.r, tint.g, tint.b, tint.a * blend);
        label.transform.localPosition = closeTextPosition * blend;
    }

    private static void SetSpritePose(SpriteRenderer renderer, float size, Vector3 position, Color color, float alpha)
    {
        renderer.enabled = alpha > 0f;
        Vector3 boundsSize = renderer.sprite.bounds.size;
        float scale = size / Mathf.Max(0.0001f, Mathf.Max(boundsSize.x, boundsSize.y));
        renderer.transform.localScale = Vector3.one * scale;
        renderer.transform.localPosition = position - renderer.sprite.bounds.center * scale;
        renderer.color = new Color(color.r, color.g, color.b, color.a * alpha);
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
        transitionInitialized = false;
    }
    public void Destroy() { if (root != null) Object.Destroy(root); }

    public void Animate(Vector3 anchorPosition, Camera camera, float amplitude, float frequency, Vector3 sway)
    {
        if (root == null || !root.activeSelf) return;
        // Advance once in LateUpdate, even when Show is called again for the focused target.
        transitionBlend = transitionDuration <= 0f ? (targetClose ? 1f : 0f)
            : Mathf.MoveTowards(transitionBlend, targetClose ? 1f : 0f, Time.deltaTime / transitionDuration);
        ApplyTransition();
        float bob = Mathf.Sin(Time.time * frequency * Mathf.PI * 2f + levitationPhase) * amplitude;
        // Move the entire centered group together, keeping its world anchor as the origin.
        root.transform.SetPositionAndRotation(anchorPosition + Vector3.up * bob
            + camera.transform.rotation * sway, camera.transform.rotation);
    }

    private static Sprite GetCircle()
    {
        if (circle != null) return circle;
        const int resolution = 64;
        var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        texture.name = "Spatial interaction circle";
        texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[resolution * resolution];
        for (int y = 0; y < resolution; y++)
        for (int x = 0; x < resolution; x++)
        {
            float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), Vector2.one * 32f);
            pixels[y * resolution + x] = new Color(1, 1, 1, Mathf.Clamp01(30f - distance));
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        circle = Sprite.Create(texture, new Rect(0, 0, resolution, resolution), Vector2.one * 0.5f, resolution);
        return circle;
    }
}
