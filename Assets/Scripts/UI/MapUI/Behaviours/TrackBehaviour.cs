using UnityEngine;

public class TrackBehaviour : MonoBehaviour, IWorldElementBinder, IResettable
{
    private RectTransformAnchorSO _submarineRectAnchor;
    public bool IsInitialized { get; private set; }

    public void Setup(RectTransformAnchorSO anchor)
    {
        if (IsInitialized) return;
        IsInitialized = true;
        _submarineRectAnchor = anchor;
    }

    public void Bind(IWorldMapUIElement worldElement)
    {
        if (_submarineRectAnchor != null)
        {
            _submarineRectAnchor.Value = GetComponent<RectTransform>();
        }
    }

    public void ResetState()
    {
        if (_submarineRectAnchor != null && _submarineRectAnchor.Value == GetComponent<RectTransform>())
        {
            _submarineRectAnchor.Value = null;
        }
    }
}