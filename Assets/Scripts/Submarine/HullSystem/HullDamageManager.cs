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
    [SerializeField] private float minSpawnInterval = 5f;
    [SerializeField] private float maxSpawnInterval = 15f;
    
    [Header("Event Channels")]
    [SerializeField] private HullPropertyEventSO onHullStatusChanged;

    private int ActiveCrackCount { get; set; }
    private Coroutine _spawnCoroutine;
    private readonly List<HullDamage> _pool = new List<HullDamage>();
    private float _nextSpawnTime;

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
        //StartSpawningBehaviour();
    }

    private void OnDestroy()
    {
        foreach (var crack in _pool)
        {
            if (crack != null)
                crack.OnCrackRepaired -= OnHullRepaired;
        }
    }
    
    private void StartSpawningBehaviour()
    {
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
        }
        _spawnCoroutine = StartCoroutine(SpawnHullDamage(Time.time + UnityEngine.Random.Range(minSpawnInterval, maxSpawnInterval)));
    }
    
    private void StopSpawningBehaviour()
    {
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
    }
    
    private void TrySpawnCrack()
    {
        var available = _pool.FindAll(c => !c.gameObject.activeSelf);
        if (available.Count == 0)
        {
            if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
            return;
        }
        var crack = available[UnityEngine.Random.Range(0, available.Count)];
        crack.gameObject.SetActive(true);
        ActiveCrackCount++;

        onHullStatusChanged?.RaiseEvent(CreateHullProperty());
        StartSpawningBehaviour();
    }

    private HullProperty CreateHullProperty()
    {
        return new HullProperty
        {
            maxHullDamagePosible = spawnZones.Length,
            activeHullDamage = ActiveCrackCount
        };
    }
    
    private void OnHullRepaired(HullDamage hullDamage)
    {
        ActiveCrackCount = Mathf.Max(0, ActiveCrackCount - 1);
        onHullStatusChanged?.RaiseEvent(CreateHullProperty());
    }
    
    private IEnumerator SpawnHullDamage(float interval)
    {
        yield return new WaitForSeconds(interval);
        TrySpawnCrack();
    }   
}
