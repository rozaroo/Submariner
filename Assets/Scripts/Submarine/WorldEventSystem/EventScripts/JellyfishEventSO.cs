using UnityEngine;

[CreateAssetMenu(menuName = "WorldEvents/Neutral/JellyfishEvent")]
public class JellyfishEventSO : WorldEventBehaviourSO
{
    public override void Execute()
    {
        IsActive = true;
        Log.Info("JellyfishEvent.Execute()");
    }

    public override bool CheckConditions()
    {
        return !IsActive; // Can only execute if the event is not already active
    }

    public override void EndEvent()
    {
        IsActive = false;
        Log.Info("JellyfishEvent.EndEvent()");
    }
}
