using UnityEngine;

public class WorldEventManager : MonoBehaviour
{
    [Header("Event Channel")] 
    [SerializeField] private MapIconPropertyEventChannelSO onEventIconEnteredRadius;
    [SerializeField] private MapIconPropertyEventChannelSO onEventIconLeftRadius;

    /*private void OnEnable()
    {
        onEventIconEnteredRadius.OnEventRaised += OnEventIconEnteredRadius;
        onEventIconLeftRadius.OnEventRaised += OnEventIconLeftRadius;
    }

    private void OnDisable()
    {
        onEventIconEnteredRadius.OnEventRaised -= OnEventIconEnteredRadius;
        onEventIconLeftRadius.OnEventRaised -= OnEventIconLeftRadius;
    }*/

    /*private void OnEventIconEnteredRadius(MapIcon eventIcon)
    {
        if (eventIcon != null)
        {
            Log.Info($"[OnEventIconEnteredRadius] Received Event Icon: {eventIcon.name}");
            //EventBehaviour eventBehaviour = eventIcon.GetComponent<EventBehaviour>();
            if (eventBehaviour != null)
            {
                Log.Info($"[OnEventIconEnteredRadius] Triggered World Event: {eventBehaviour.name}");
                WorldEventBehaviourSO eventSo = eventBehaviour.worldEvent;
                if (eventSo != null)
                {
                    if(eventSo.CheckConditions())
                    {
                        eventSo.Execute();
                    }
                }
            }
        }
    }*/

    /*private void OnEventIconLeftRadius(MapIcon eventIcon)
    {
        if (eventIcon != null)
        {
            Log.Info($"[OnEventIconLeftRadius] Received Event Icon: {eventIcon.name}");
            EventBehaviour eventBehaviour = eventIcon.GetComponent<EventBehaviour>();
            if (eventBehaviour != null)
            {
                Log.Info($"[OnEventIconLeftRadius] Left World Event: {eventBehaviour.name}");
                WorldEventBehaviourSO eventSo = eventBehaviour.worldEvent;
                if (eventSo != null)
                {
                    eventSo.EndEvent();
                }
            }
        }
    }*/
}
