using UnityEngine;

public class MainWorldEvent : WorldMapUIElement, IMainWorldEvent
{
    [Header("Feedback")]
    [SerializeField] private GameObject visualMarker;
    [SerializeField] private string completeSfxEvent = "Start_MainEventTrigger";
    [SerializeField] private bool destroyOnComplete = false;
    
    private string _eventName;
    private string _objectiveDescription;
    private bool _completed;
    
    public string ObjectiveDescription => _objectiveDescription;
    
    public void InjectMissionData(string name, string description)
    {
        _eventName = name;
        _objectiveDescription = description;
    }

    public bool CheckConditions() => !_completed;

    public void Execute()
    {
        if (_completed) return;
        _completed = true;

        Log.Info($"[MainWorldEvent] '{_eventName}' activated.");

        /*if (!string.IsNullOrEmpty(completeSfxEvent))
            SFXManager.PostEvent(completeSfxEvent, gameObject);*/

        if (visualMarker != null)
            visualMarker.SetActive(false);

        if (destroyOnComplete)
        {
            if (TryGetComponent(out Collider col))
            {
                col.enabled = false;
            }
            enabled = false; 
        }
    }

    public void EndEvent() { }
}