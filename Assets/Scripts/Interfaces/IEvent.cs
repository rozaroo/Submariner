using UnityEngine;

public interface IEvent
{
    public bool IsActive { get; set; }
    public void Execute();
    public bool CheckConditions();
    public void EndEvent();
}
