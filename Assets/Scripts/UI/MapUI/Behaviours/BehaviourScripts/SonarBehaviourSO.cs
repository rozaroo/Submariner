using UnityEngine;

[CreateAssetMenu(menuName = "Map/Behaviours/Sonar")]
public class SonarBehaviourSO : IconBehaviourSO
{
    [SerializeField] private Sprite sonarIcon;
    [SerializeField] private float _radius = 50f;
    [SerializeField] private float _timePerSonarCheck = 0.2f;
    [SerializeField] private Color _color = Color.cyan;
    public override void ApplyComponent(GameObject go)
    {
        var comp = go.AddComponent<SonarBehaviour>();
        if(comp != null)
        {
            comp.Setup(_radius, _timePerSonarCheck, _color, sonarIcon);
        }
    }
}