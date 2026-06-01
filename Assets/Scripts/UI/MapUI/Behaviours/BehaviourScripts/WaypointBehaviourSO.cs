using UnityEngine;

[CreateAssetMenu(menuName = "Map/Behaviours/Waypoint")]
public class WaypointBehaviourComponentSo : IconBehaviourSO
{
    public override void ApplyComponent(GameObject go)
    {
        var comp = go.AddComponent<WaypointBehaviour>();
        if (comp is ISetup setup)
        {
            setup.Setup();
        }
    }
}