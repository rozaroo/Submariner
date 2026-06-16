using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HullDamageManager : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject hullDamagePrefab;

    [Header("Spawn Zones")]
    [SerializeField] private Transform[] spawnZones;

    [Header("Spawn Parameters")]
    [SerializeField] private float gracePeriod = 10f;
    [SerializeField] private float minSpawnInterval = 5f;
    [SerializeField] private float maxSpawnInterval = 15f;

    private int ActiveCrackCount { get; set; }
    private Coroutine _spawnCoroutine;
    private readonly List<HullDamage> _pool = new List<HullDamage>();

    private void Start()
    {
        foreach (var zone in spawnZones)
        {
            var go = Instantiate(hullDamagePrefab, zone.position, zone.rotation);
            go.SetActive(false);
            var crack = go.GetComponent<HullDamage>();
            crack.OnCrackRepaired += OnHullRepaired;
            _pool.Add(crack);
        }
    }

    private void OnEnable()
    {
        GameEventChannel<OnSubmarineCollision>.OnEventRaised += OnSubmarineCollision;
    }

    private void OnDisable()
    {
        GameEventChannel<OnSubmarineCollision>.OnEventRaised -= OnSubmarineCollision;
    }

    private void OnDestroy()
    {
        foreach (var crack in _pool)
            if (crack != null) crack.OnCrackRepaired -= OnHullRepaired;
    }
    
    /// <summary>
    /// Maybe remove the Grace Period.
    /// </summary>
    [ContextMenu("Start Hull Grace Period")]
    private void OnStartGracePeriod()
    {
        StartCoroutine(StartHullGracePeriod());
    }

    private IEnumerator StartHullGracePeriod()
    {
        yield return new WaitForSeconds(gracePeriod);
        StartSpawningBehaviour();
    }
    
    private void OnSubmarineCollision(OnSubmarineCollision collision)
    {
        SFXManager.PostEvent("Start_SubmarineCollision", gameObject);
        StartSpawningBehaviour();
    }

    private void StartSpawningBehaviour()
    {
        if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
        
        TrySpawnCrack(); 
        
        _spawnCoroutine = StartCoroutine(SpawnHullDamageRoutine());
    }

    private void StopSpawningBehaviour()
    {
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnHullDamageRoutine()
    {
        while (true) 
        {
            yield return new WaitForSeconds(Random.Range(minSpawnInterval, maxSpawnInterval));
            TrySpawnCrack();
        }
    }
    
    private void TrySpawnCrack()
    {
        var available = _pool.FindAll(c => !c.gameObject.activeSelf);
    
        if (available.Count == 0) 
        {
            StopSpawningBehaviour();
            return;
        }
    
        Log.Info("Spawned Crack");
    
        var crack = available[Random.Range(0, available.Count)];
        crack.gameObject.SetActive(true);
        ActiveCrackCount++;

        GameEventChannel<OnHullPropertyChange>.RaiseEvent(new OnHullPropertyChange(spawnZones.Length, ActiveCrackCount));
    }

    private void OnHullRepaired(HullDamage hullDamage)
    {
        ActiveCrackCount = Mathf.Max(0, ActiveCrackCount - 1);
        GameEventChannel<OnHullPropertyChange>.RaiseEvent(new OnHullPropertyChange(spawnZones.Length, ActiveCrackCount));
        
        if (ActiveCrackCount == 0)
        {
            StopSpawningBehaviour();
        }
    }
    
}
