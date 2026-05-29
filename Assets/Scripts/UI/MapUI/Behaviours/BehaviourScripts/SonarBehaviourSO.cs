using UnityEngine;

[CreateAssetMenu(menuName = "Map/Behaviours/Sonar")]
public class SonarBehaviourSO : IconBehaviourSO
{
    [SerializeField] private float _generalRadius = 50f;
    [SerializeField] private float _timePerSonarCheck = 0.2f;
    [SerializeField] private Sprite _sonarIconSprite;
    [SerializeField] private Color _mainSonarColor = Color.yellow;
    [SerializeField] private Color _secondarySonarColor = Color.green;
    public override void ApplyComponent(GameObject go)
    {
        var comp = go.AddComponent<SonarBehaviour>();
        if(comp != null)
        {
            comp.Setup(_generalRadius, _timePerSonarCheck, _mainSonarColor, _secondarySonarColor, _sonarIconSprite);
        }
    }
}