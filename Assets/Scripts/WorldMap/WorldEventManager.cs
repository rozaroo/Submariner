using UnityEngine;

public class WorldEventManager : MonoBehaviour
{
    [Header("Event Channel")] 
    [SerializeField] private SonarElementsDetectionEventChannelSO onOuterRadarChanged;
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
    
    private void OnOutterRadarStateChanged(SonarElementsDetectionProperty property)
    {
        //TODO: Currently not used, but can be implemented similarly to inner radar if needed
    }

    private void OnInnerRadarStateChanged(SonarElementsDetectionProperty property)
    {
        if (property.WorldElement == null) return;

        if (property.WorldElement is not IEvent worldEvent) return;
        
        if (property.IsRevealed)
        {
            Log.Info($"[WorldEventManager] Entered Radius of: {property.WorldElement}");
            
            if (worldEvent.CheckConditions())
            {
                worldEvent.Execute();
            }
        }
        else
        {
            Log.Info($"[WorldEventManager] Left Radius of: {property.WorldElement}");
            worldEvent.EndEvent();
        }
    }
}