using UnityEngine;

public class WorldEventManager : MonoBehaviour
{
    [Header("Event Channel")] 
    [SerializeField] private SonarElementsDetectionEventChannelSO onInnerRadarChanged;

    private void OnEnable()
    {
        if (onInnerRadarChanged != null)
            onInnerRadarChanged.OnEventRaised += OnInnerRadarStateChanged;
    }

    private void OnDisable()
    {
        if (onInnerRadarChanged != null)
            onInnerRadarChanged.OnEventRaised -= OnInnerRadarStateChanged;
    }

    private void OnInnerRadarStateChanged(SonarElementsDetectionProperty property)
    {
        if (property.WorldElement == null) return;
        
        var worldMono = property.WorldElement as MonoBehaviour;
        if (worldMono == null) return;
        
        IEvent worldEvent = worldMono.GetComponent<IEvent>();
        if (worldEvent == null) return;
        
        if (property.IsRevealed)
        {
            Log.Info($"[WorldEventManager] Entered Radius of: {worldMono.name}");
            
            if (worldEvent.CheckConditions())
            {
                worldEvent.Execute();
            }
        }
        else
        {
            Log.Info($"[WorldEventManager] Left Radius of: {worldMono.name}");
            worldEvent.EndEvent();
        }
    }
}