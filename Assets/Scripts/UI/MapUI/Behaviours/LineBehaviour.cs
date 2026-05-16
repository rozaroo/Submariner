using System;
using UnityEngine;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

public class LineBehaviour : MonoBehaviour, ISetup
{
    private float lineWidth = 5f;
    private Color lineColor = Color.crimson;
    private GameObject _lineObject;
    private RectTransform _selfRectTransform;
    private RectTransform _targetRectTransform;
    private Image lineImage;
    public bool IsInitialized { get; }

    private void Awake()
    {
        _lineObject = new GameObject("LineObject");
        lineImage = _lineObject.AddComponent<Image>();
        lineImage.color = lineColor;
        _selfRectTransform = _lineObject.GetComponent<RectTransform>();
    }

    public void Setup() => Setup(lineWidth, lineColor, null);

    public void Setup(float width, Color color, Material material)
    {
        lineWidth = width;
        lineColor = color;
        lineImage.color = lineColor;
        if (material != null)
            lineImage.material = material;
    }

    public void SetContainer(GameObject container)
    {
        if(container != null)
            _lineObject.transform.SetParent(container.transform,false);
    }

    public void SetTarget(RectTransform _origin, RectTransform target)
    {
        var origin = _origin;
        _targetRectTransform = target;
        SetPosition(origin);
        SetRotation(origin);
        SetScale(origin);
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
        Log.Info(distance.ToString());
        _selfRectTransform.sizeDelta = new Vector2(distance, lineWidth);
    }

    public void OnDestroyLine()
    {
        Destroy(_lineObject);
        Destroy(gameObject);
    }
}
