using System.Collections.Generic;
using UnityEngine;

public class FlockingCore : MonoBehaviour, ISetup
{
    private FlockingSettingsSO _settings;
    private GameObject _agentPrefab;

    private List<FlockAgent> _agents = new List<FlockAgent>();
    private Transform _managerTransform;
    private bool _hasSpawned = false;
    public bool IsInitialized { get; private set; }
    private void Start()
    {
        _managerTransform = transform;
        SpawnGroup();
    }
    
    public void Setup() => Setup(_settings, _agentPrefab);
    
    public void Setup(FlockingSettingsSO settings, GameObject prefab)
    {
        if (IsInitialized) return;
        IsInitialized = true;

        _settings = settings;
        _agentPrefab = prefab;
    }
    
    private void Update()
    {
        if (_agents.Count <= 0 || _settings == null) return;

        float sqrNeighborRadius = _settings.neighborRadius * _settings.neighborRadius;
        float sqrAvoidanceRadius = _settings.avoidanceRadius * _settings.avoidanceRadius;

        for (int i = 0; i < _agents.Count; i++)
        {
            FlockAgent currentAgent = _agents[i];
            Vector3 currentPos = currentAgent.transform.position;

            Vector3 cohesionVector = Vector3.zero;
            Vector3 alignmentVector = Vector3.zero;
            Vector3 avoidanceVector = Vector3.zero;

            int neighborCount = 0;

            for (int j = 0; j < _agents.Count; j++)
            {
                if (i == j) continue;

                FlockAgent neighbor = _agents[j];
                Vector3 neighborPos = neighbor.transform.position;
                
                float sqrDist = (neighborPos - currentPos).sqrMagnitude;

                if (sqrDist < sqrNeighborRadius)
                {
                    neighborCount++;
                    cohesionVector += neighborPos;
                    alignmentVector += neighbor.Velocity;

                    if (sqrDist < sqrAvoidanceRadius)
                    {
                        avoidanceVector += (currentPos - neighborPos);
                    }
                }
            }

            Vector3 totalAcceleration = Vector3.zero;

            if (neighborCount > 0)
            {
                cohesionVector /= neighborCount;
                cohesionVector -= currentPos;
                totalAcceleration += cohesionVector.normalized * _settings.cohesionWeight;

                alignmentVector /= neighborCount;
                totalAcceleration += alignmentVector.normalized * _settings.alignmentWeight;

                totalAcceleration += avoidanceVector.normalized * _settings.avoidanceWeight;
            }

            Vector3 offsetToCenter = _managerTransform.position - currentPos;
            if (offsetToCenter.sqrMagnitude > _settings.containmentRadius * _settings.containmentRadius)
            {
                totalAcceleration += offsetToCenter.normalized * _settings.boundsWeight;
            }

            currentAgent.Move(totalAcceleration, _settings);
        }
    }

    public void SetGroupVisibility(bool visible)
    {
        if (visible && !_hasSpawned)
        {
            _managerTransform = transform;
            SpawnGroup();
            _hasSpawned = true;
            return;
        }

        for (int i = 0; i < _agents.Count; i++)
        {
            if (_agents[i] != null)
            {
                _agents[i].gameObject.SetActive(visible);
                if (visible)
                {
                    _agents[i].Initialize(_agents[i].transform.forward * _settings.minSpeed);
                }
            }
        }
    }
    
    private void SpawnGroup()
    {
        if (_agentPrefab == null || _settings.spawnAmount <= 0) return;

        for (int i = 0; i < _settings.spawnAmount; i++)
        {
            Vector3 randomPos = _managerTransform.position + Random.insideUnitSphere * _settings.spawnRadius;
            
            GameObject go = Instantiate(_agentPrefab, randomPos, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
            go.transform.SetParent(_managerTransform);

            FlockAgent agent = go.GetComponent<FlockAgent>();
            if (agent == null)
            {
                agent = go.AddComponent<FlockAgent>();
            }
            agent.Initialize(go.transform.forward * _settings.minSpeed);
            _agents.Add(agent);
        }
    }

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if(_settings != null)
            Gizmos.DrawWireSphere(transform.position, _settings.containmentRadius);
    }
    #endif
}