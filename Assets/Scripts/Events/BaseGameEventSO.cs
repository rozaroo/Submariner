using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Base Event", menuName = "Events/Base Event (Void)")]
public class BaseEventChannelSO : ScriptableObject
{
    public event Action OnEventRaised;

    public void RaiseEvent()
    {
        OnEventRaised?.Invoke();
    }
}