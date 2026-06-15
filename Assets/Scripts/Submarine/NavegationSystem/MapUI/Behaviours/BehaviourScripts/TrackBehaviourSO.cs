using UnityEngine;

[CreateAssetMenu(fileName = "Track Behaviour Config", menuName = "MapUI/Behaviours/Track Visual")]
public class TrackBehaviourSO : IconBehaviourSO
{
    [Header("Dependencies")]
    [SerializeField] private RectTransformAnchorSO submarineRectAnchor;

    public override void ApplyComponent(GameObject go)
    {
        var comp = go.AddComponent<TrackBehaviour>();
        if (comp != null)
        {
            comp.Setup(submarineRectAnchor);
        }
    }
}