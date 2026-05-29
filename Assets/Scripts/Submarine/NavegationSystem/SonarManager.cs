using System.Collections.Generic;
using UnityEngine;

public class SonarManager : MonoBehaviour
{
    [Header("Event Channel")] 
    [SerializeField] private MapIconPropertyEventChannelSO onSubmarineCreated;
    [SerializeField] private MapIconListPropertyEventChannelSO onMapUpdated;
    
    private SonarBehaviour _sonarBehaviour;
    private readonly List<MapIcon> _mapEventList = new List<MapIcon>();

    private void OnEnable()
    {
        onSubmarineCreated.OnEventRaised += OnTargetUpdated;
        onMapUpdated.OnEventRaised += OnListUpdated;
    }
    
    private void OnDisable()
    {
        onSubmarineCreated.OnEventRaised -= OnTargetUpdated;
        onMapUpdated.OnEventRaised -= OnListUpdated;
    }
    
    private void OnTargetUpdated(MapIcon targetIcon)
    {
        if (targetIcon != null)
        {
            Log.Info("[OnTargetUpdated] Received Sonar Behaviour");
            _sonarBehaviour = targetIcon.gameObject.GetComponent<SonarBehaviour>();
        }
        SendFilteredData();
    }
    
    private void OnListUpdated(List<MapIcon> mapIcons)
    {
        FilterList(mapIcons);
    }
    
    private void FilterList(List<MapIcon> mapIcons)
    {
        foreach (MapIcon mapIcon in mapIcons)
        {
            if (mapIcon.GetComponent<EventBehaviour>() != null)
            {
                _mapEventList.Add(mapIcon);
            }
        }
        SendFilteredData();
    }
    
    private void SendFilteredData()
    {
        if (_sonarBehaviour != null && _mapEventList.Count > 0)
        {
            Log.Info("[SendFilteredData] Sent Map Event List to Sonar Behaviour");
            _sonarBehaviour.MapEventList = _mapEventList;
        }
    }
}
