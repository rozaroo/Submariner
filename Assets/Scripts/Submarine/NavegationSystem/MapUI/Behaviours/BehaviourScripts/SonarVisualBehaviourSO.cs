using UnityEngine;

[CreateAssetMenu(menuName = "MapUI/Behaviours/SonarVisual")]
public class SonarVisualBehaviourSO : IconBehaviourSO
{
    [Header("Dependencies")]
    [SerializeField] private MapRuntimeDataSO mapRuntimeData;
    
    [Header("Visuals")]
    [SerializeField] private Sprite sonarIconSprite;
    [SerializeField] private Color mainSonarColor = new Color(1f, 1f, 0f, 0.3f);
    [SerializeField] private Color secondarySonarColor = new Color(0f, 1f, 0f, 0.3f);

    public override void ApplyComponent(GameObject go)
    {
        var comp = go.AddComponent<SonarVisualBehaviour>();
        if (comp != null)
        {
            comp.Setup(sonarIconSprite, mainSonarColor, secondarySonarColor, mapRuntimeData);
        }
    }
}