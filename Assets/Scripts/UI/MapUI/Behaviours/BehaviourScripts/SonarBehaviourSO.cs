using UnityEngine;

[CreateAssetMenu(menuName = "Map/Behaviours/Sonar")]
public class SonarBehaviourSO : IconBehaviourSO
{
    [SerializeField] private float generalRadius = 50f;
    [SerializeField] private float timePerSonarCheck = 0.2f;
    [SerializeField] private Sprite sonarIconSprite;
    [SerializeField] private Color mainSonarColor = Color.yellow;
    [SerializeField] private Color secondarySonarColor = Color.green;
    [SerializeField] private MapIconPropertyEventChannelSO onEventIconEnteredRadius;
    [SerializeField] private MapIconPropertyEventChannelSO onEventIconLeftRadius;
    public override void ApplyComponent(GameObject go)
    {
        var comp = go.AddComponent<SonarBehaviour>();
        if(comp != null)
        {
            comp.Setup(generalRadius, timePerSonarCheck, mainSonarColor, secondarySonarColor, sonarIconSprite, onEventIconEnteredRadius, onEventIconLeftRadius);
        }
    }
}