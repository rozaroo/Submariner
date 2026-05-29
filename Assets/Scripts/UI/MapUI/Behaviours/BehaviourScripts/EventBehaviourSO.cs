using UnityEngine;

[CreateAssetMenu(menuName = "Map/Behaviours/Event")]
public class EventBehaviourSO : IconBehaviourSO
{
    private WorldEventSO worldEventSo;
    public override void ApplyComponent(GameObject go)
    {
        var comp = go.AddComponent<EventBehaviour>();
        comp.Setup(worldEventSo);
    }
}