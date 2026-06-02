using UnityEngine;

[CreateAssetMenu(fileName = "New RectTransform Anchor", menuName = "MapUI/Anchors/RectTransform Anchor")]
public class RectTransformAnchorSO : ScriptableObject
{
    // Propiedad en caliente que guardará el RectTransform del submarino en ejecución
    public RectTransform Value { get; set; }
}