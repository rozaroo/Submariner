using UnityEngine;
public abstract class WorldEventBehaviourSO : IconBehaviourSO
{
    private void OnEnable() => IsActive = false;
    protected bool IsActive { get; set; }
    
    public override void ApplyComponent(GameObject go)
    {
        var comp = go.AddComponent<EventBehaviour>();
        comp.Setup(this);
    }
    
    public abstract void Execute();
    public abstract bool CheckConditions();
    public abstract void EndEvent();
}