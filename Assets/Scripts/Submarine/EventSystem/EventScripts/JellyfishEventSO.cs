using UnityEngine;

[CreateAssetMenu(menuName = "WorldEvents/Neutral/JellyfishEvent")]
public class JellyfishEvent : WorldEventSO
{
    public override void Execute()
    {
        Log.Info("JellyfishEvent.Execute()");
    }
}
