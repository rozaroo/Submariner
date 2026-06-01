using UnityEngine;
using Image = UnityEngine.UI.Image;

public class LineBehaviour : MonoBehaviour, ISetup, IResettable
{
    private float _lineWidth = 5f;
    private Color _lineColor = Color.crimson;
    private GameObject _lineObject;
    
    private RectTransform _selfRectTransform;
    private RectTransform _originRectTransform;
    private RectTransform _targetRectTransform;
    
    private Image _lineImage;
    public bool IsInitialized { get; private set; }

    private void Awake()
    {
        _lineObject = new GameObject("LineObject");
        _lineImage = _lineObject.AddComponent<Image>();
        _lineImage.color = _lineColor;
        _selfRectTransform = _lineObject.GetComponent<RectTransform>();
    }

    public void Setup() => Setup(_lineWidth, _lineColor, null);

    public void Setup(float width, Color color, Material material)
    {
        if (IsInitialized) return;
        IsInitialized = true;
        _lineWidth = width;
        _lineColor = color;
        _lineImage.color = _lineColor;
        if (material != null)
            _lineImage.material = material;
    }

    public void SetContainer(GameObject container)
    {
        if(container != null)
            _lineObject.transform.SetParent(container.transform,false);
    }

    public void SetTarget(RectTransform origin, RectTransform target)
    {
        _originRectTransform = origin;
        _targetRectTransform = target;
        
        _lineObject.SetActive(true); 
        UpdateLineTransform(); 
    }
    
    private void LateUpdate()
    {
        if (_lineObject != null && _lineObject.activeSelf && 
            _originRectTransform != null && _targetRectTransform != null)
        {
            UpdateLineTransform();
        }
    }
    
    private void UpdateLineTransform()
    {
        SetPosition(_originRectTransform);
        SetRotation(_originRectTransform);
        SetScale(_originRectTransform);
    }
    
    [ContextMenu("RectTransform/Set Position")]
    public void SetPosition(RectTransform origin)
    {
        Vector2 midpoint = (origin.anchoredPosition + _targetRectTransform.anchoredPosition) / 2f;
        _selfRectTransform.anchoredPosition = midpoint;
    }

    [ContextMenu("RectTransform/Set Rotation")]
    public void SetRotation(RectTransform origin)
    {
        Vector2 dir = _targetRectTransform.anchoredPosition - origin.anchoredPosition;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        
        _selfRectTransform.localRotation = Quaternion.Euler(
            _selfRectTransform.localEulerAngles.x,
            _selfRectTransform.localEulerAngles.y,
            angle
        );
    }

    [ContextMenu("RectTransform/Set Scale")]
    public void SetScale(RectTransform origin)
    {
        float distance = Vector2.Distance(origin.anchoredPosition, _targetRectTransform.anchoredPosition);
        _selfRectTransform.sizeDelta = new Vector2(distance, _lineWidth);
    }
    

    public void ResetState()
    {
        if (_lineObject != null)
        {
            _lineObject.SetActive(false);
        }
        _targetRectTransform = null; 
        _originRectTransform = null;
    }
}
