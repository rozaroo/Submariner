using UnityEngine;
using UnityEngine.Serialization;

public class DrainageStation : MonoBehaviour, IPossessable
{
    [Header("Drainage References")]
    [SerializeField] private GameObject _drainageLever;
    
    [Header("Drainage Settings")]
    [SerializeField] private float _drainageSpeed;
    [SerializeField] private bool _isDraining;
    
    [Header("Events Channels")] 
    [SerializeField] private EnergyStatusEventSO _energyStatusEventSO;
    
    public void Possess()
    {
        
    }

    public void UnPossess()
    {
    }
}
