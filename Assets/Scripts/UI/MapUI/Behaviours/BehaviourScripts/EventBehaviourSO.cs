using UnityEngine;

[CreateAssetMenu(menuName = "Map/Behaviours/Event")]
public class EventBehaviourSO : IconBehaviourSO
{
    public override void ApplyComponent(GameObject go)
    {
        var comp = go.AddComponent<EventBehaviour>();
        if (comp is ISetup setup)
        {
            setup.Setup();
        }
    }
}