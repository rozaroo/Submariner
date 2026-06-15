using UnityEngine;

[CreateAssetMenu(fileName = "New RectTransform Anchor", menuName = "MapUI/Anchors/RectTransform Anchor")]
public class RectTransformAnchorSO : ScriptableObject
{
    public RectTransform Value { get; set; }
}