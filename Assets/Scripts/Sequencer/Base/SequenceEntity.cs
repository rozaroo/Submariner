using UnityEngine;
using UnityEngine.Events;

public class SequenceEntity : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Use UNIQUE id per object in Blackboard")]
    public string Id;
    
    [Header("Sequence State Reactions")]
    [Tooltip("What Happens when it Enters.")]
    public UnityEvent OnSequenceEnter;
    
    [Tooltip("What Happens when it Exits.")]
    public UnityEvent OnSequenceExit;
    
    #if UNITY_EDITOR
    [ContextMenu("Create ID")]
    private void CreateId()
    {
        Id = gameObject.name;
    }
    #endif
}
