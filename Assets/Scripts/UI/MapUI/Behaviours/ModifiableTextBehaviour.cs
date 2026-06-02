using TMPro;
using UnityEngine;

public class ModifiableTextBehaviour : MonoBehaviour, ISetup, IResettable
{
    [SerializeField] private string _textDisplay;
    [SerializeField] private float _fontSize = 2f;
    [SerializeField] private TextAlignmentOptions _alignment = TextAlignmentOptions.Center;

    private GameObject _textGo;
    private RectTransform _textRectTransform;
    private TextMeshProUGUI _textComponent;
    public bool IsInitialized { get; private set; }
    public float FontSize
    {
        set
        {
            _fontSize = value;
            if (_textComponent != null)
                _textComponent.fontSize = value;
        }
    }

    private void Awake()
    {
        _textGo = new GameObject("Text");
        _textGo.transform.SetParent(transform, false);
        _textComponent = _textGo.AddComponent<TextMeshProUGUI>();
        _textRectTransform = _textGo.GetComponent<RectTransform>();
    }
    
    public void Setup() => Setup(_textDisplay, _fontSize, _alignment);

    public void Setup(string text, float fontSize, TextAlignmentOptions alignment)
    {
        if (IsInitialized) return;
        IsInitialized = true;
        
        _textGo.transform.localPosition = Vector3.zero;
        _textGo.transform.localRotation = Quaternion.identity;
        _textGo.transform.localScale    = Vector3.one;
        
        _textComponent.text      = text;
        _textComponent.fontSize  = fontSize;
        _textComponent.alignment = alignment;
        
        _textRectTransform.anchorMin        = Vector2.zero;
        _textRectTransform.anchorMax        = Vector2.one;
        _textRectTransform.pivot            = new Vector2(0.5f, 0.5f);
        _textRectTransform.sizeDelta        = Vector2.zero;
        _textRectTransform.anchoredPosition = Vector2.zero;
    }

    public void ResetState()
    {
        if (_textComponent != null)
        {
            _textComponent.text = string.Empty;
        }
    }
}