using System.Collections.Generic;
using UnityEngine;

public class SonarManager : MonoBehaviour
{
    private SubmarineSonar _submarineSonar;
    private readonly List<IWorldElement> _worldEntities = new List<IWorldElement>();

    private void OnEnable()
    {
        GameEventChannel<OnWorldMapGeneratedProperty>.OnEventRaised += OnMapGenerated;
        GameEventChannel<OnWorldSubmarineGenerated>.OnEventRaised += OnSubmarineGenerated;
        
        GameEventChannel<OnWorldMapElementGenerated>.OnEventRaised += OnWorldElementGenerated;
    }
    
    private void OnDisable()
    {
        GameEventChannel<OnWorldMapGeneratedProperty>.OnEventRaised -= OnMapGenerated;
        GameEventChannel<OnWorldSubmarineGenerated>.OnEventRaised -= OnSubmarineGenerated;
        
        GameEventChannel<OnWorldMapElementGenerated>.OnEventRaised -= OnWorldElementGenerated;
    }

    private void OnMapGenerated(OnWorldMapGeneratedProperty data)
    {
        _worldEntities.Clear();
        foreach (var element in data.MapElements)
        {
            _worldEntities.Add(element);
        }
        TryInitializeSonar();
    }
    
    private void OnWorldElementGenerated(OnWorldMapElementGenerated data)
    {
        if (!_worldEntities.Contains(data._worldElementGenerated))
        {
            _worldEntities.Add(data._worldElementGenerated);
            
            if (_submarineSonar != null)
            {
                TryInitializeSonar();
            }
        }
    }

    private void OnSubmarineGenerated(OnWorldSubmarineGenerated submarineElement)
    {
        var submarineMono = submarineElement._submarineElement as MonoBehaviour;
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