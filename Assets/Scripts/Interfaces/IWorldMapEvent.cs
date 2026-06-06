using UnityEngine;

public interface IWorldMapEvent
{
    public bool CheckConditions();
    public void Execute();
    public void EndEvent();
}
