using System.Collections.Generic;
using UnityEngine;

public class SonarManager : MonoBehaviour
{
    [Header("Event Channels")] 
    [SerializeField] private WorldMapGeneratedPropertyEventChannelSO onWorldMapGenerated;
    [SerializeField] private WorldMapUIElementEventChannelSO onWorldSubmarineGenerated;
    
    private SubmarineSonar _submarineSonar;
    private readonly List<IWorldElement> _worldEntities = new List<IWorldElement>();

    private void OnEnable()
    {
        onWorldMapGenerated.OnEventRaised += OnMapGenerated;
        onWorldSubmarineGenerated.OnEventRaised += OnSubmarineGenerated;
    }
    
    private void OnDisable()
    {
        onWorldMapGenerated.OnEventRaised -= OnMapGenerated;
        onWorldSubmarineGenerated.OnEventRaised -= OnSubmarineGenerated;
    }

    private void OnMapGenerated(WorldMapGeneratedProperty data)
    {
        _worldEntities.Clear();
        foreach (var element in data.mapElements)
        {
            _worldEntities.Add(element);
        }
        TryInitializeSonar();
    }
    
    private void OnSubmarineGenerated(IWorldMapUIElement submarineElement)
    {
        var submarineMono = submarineElement as MonoBehaviour;
        if (submarineMono != null)
        {
            _submarineSonar = submarineMono.GetComponent<SubmarineSonar>();
            Log.Info("[SonarManager] Submarine Sonar Linked.");
        }
        
        TryInitializeSonar();
    }
    
    private void TryInitializeSonar()
    {
        if (_submarineSonar != null && _worldEntities.Count > 0)
        {
            Log.Info($"[SonarManager] Initializing Sonar with {_worldEntities.Count} physical targets.");
            _submarineSonar.InitializeSonarTargets(_worldEntities);
        }
        else
        {
            Log.Warning("[SonarManager] Cannot initialize Sonar. Submarine Sonar or World Entities not ready.");
        }
    }
}