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

    private void OnDestroy()
    {
        foreach (var crack in _pool)
            if (crack != null) crack.OnCrackRepaired -= OnHullRepaired;
    }

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

    private void StartSpawningBehaviour()
    {
        if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
        _spawnCoroutine = StartCoroutine(SpawnHullDamage(Random.Range(minSpawnInterval, maxSpawnInterval)));
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
        if (available.Count == 0) return;

        var crack = available[Random.Range(0, available.Count)];
        crack.gameObject.SetActive(true);
        ActiveCrackCount++;

        GameEventChannel<OnHullPropertyChange>.RaiseEvent(CreateHullProperty());
        StartSpawningBehaviour();
    }

    private OnHullPropertyChange CreateHullProperty()
    {
        return new OnHullPropertyChange
        {
            maxHullDamagePosible = spawnZones.Length,
            activeHullDamage     = ActiveCrackCount
        };
    }

    private void OnHullRepaired(HullDamage hullDamage)
    {
        ActiveCrackCount = Mathf.Max(0, ActiveCrackCount - 1);
        GameEventChannel<OnHullPropertyChange>.RaiseEvent(CreateHullProperty());
    }

    private IEnumerator SpawnHullDamage(float interval)
    {
            yield return new WaitForSeconds(interval);
        TrySpawnCrack();
    }
}
