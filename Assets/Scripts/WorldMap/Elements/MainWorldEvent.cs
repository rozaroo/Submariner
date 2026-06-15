using UnityEngine;
public class MainWorldEvent : WorldMapUIElement, IMainWorldEvent
{
    [Header("Main Event Info")]
    [SerializeField] private string eventName = "Main Event";

    [Header("Feedback")]
    [SerializeField] private GameObject visualMarker;
    [SerializeField] private string completeSfxEvent = "Start_MainEventTrigger";
    [SerializeField] private bool destroyOnComplete = false;

    private bool _completed;

    public bool CheckConditions() => !_completed;

    public void Execute()
    {
        if (_completed) return;
        _completed = true;

        Log.Info($"[MainWorldEvent] '{eventName}' activated.");

        if (!string.IsNullOrEmpty(completeSfxEvent))
            SFXManager.PostEvent(completeSfxEvent, gameObject);

        if (visualMarker != null)
            visualMarker.SetActive(false);

        if (destroyOnComplete)
            Destroy(gameObject);
    }

    public void EndEvent() { }
}