using UnityEngine;

[CreateAssetMenu(menuName = "Map/Behaviours/Waypoint")]
public class WaypointBehaviourSO : IconBehaviourSO
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