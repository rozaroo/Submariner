using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GameEvents", menuName = "Scriptable Objects/EnergyEventsSO")]
public class EventsSO : ScriptableObject
{
    public event Action OnEvent;

    public void CallEvent()
    {
        OnEvent?.Invoke();
    }
}
