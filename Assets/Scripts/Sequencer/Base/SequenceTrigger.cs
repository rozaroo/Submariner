using System;
using UnityEngine;

public class SequenceTrigger : MonoBehaviour
{
    public event Action OnTriggerActivated;
    [SerializeField] private bool triggerOnce = true;
    private bool _hasBeenTriggered = false;
    
    public void SetActiveState(bool state)
    {
        gameObject.SetActive(state);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && _hasBeenTriggered) return;
        _hasBeenTriggered = true;
        OnTriggerActivated?.Invoke();
    }
}