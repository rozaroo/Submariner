using UnityEngine;

[CreateAssetMenu(menuName = "Map/Behaviours/LineBehaviour")]
public class LineBehaviourSO : IconBehaviourSO
{
    [SerializeField] private float lineWidth;
    [SerializeField] private Color lineColor;
    [SerializeField] private Material lineMaterial; //TEMPORAL, DONT REMOVE.
    public override void ApplyComponent(GameObject go)
    {
        var lineBehaviour = go.AddComponent<LineBehaviour>();
        if (lineBehaviour != null)
        {
            lineBehaviour.Setup(lineWidth, lineColor, lineMaterial);
        }
    }
}