using UnityEngine;

public class WorldEventManager : MonoBehaviour
{

    private void OnEnable()
    {
        GameEventChannel<OnSonarElementsDetection>.OnEventRaised += OnRadarStateChanged;
    }

    private void OnDisable()
    {
        GameEventChannel<OnSonarElementsDetection>.OnEventRaised -= OnRadarStateChanged;
    }
    

    private void OnRadarStateChanged(OnSonarElementsDetection property)
    {
        if (property.WorldElement == null) return;
        if (property.WorldElement is not IWorldMapEvent worldEvent) return;
        
        if (property.SonarRegion != SonarDetectionMode.InnerOnly) return;
        
        if (property.IsRevealed)
        {
            if (worldEvent.CheckConditions())
            {
                worldEvent.Execute();   
            }
        }
        else
        {
            worldEvent.EndEvent();
        }
    }
}