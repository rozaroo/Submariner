using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HullDamageManager : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private List<GameObject> hullDamageGOs;

    [Header("Spawn Parameters")]
    [SerializeField] private float gracePeriod = 10f;
    [SerializeField] private float minSpawnInterval = 5f;
    [SerializeField] private float maxSpawnInterval = 15f;

    [Header("Debug")]
    [Tooltip("Marca esta casilla en el Inspector (en Play Mode) para forzar la aparici�n de una grieta instant�neamente.")]
    [SerializeField] private bool forceHullDamageTrigger;

    private int ActiveCrackCount { get; set; }
    private Coroutine _spawnCoroutine;
    private readonly List<HullDamage> _pool = new List<HullDamage>();

    private void Start()
    {
        foreach (var prefab in hullDamageGOs)
        {
            if (prefab == null) continue;

            var crack = prefab.GetComponentInChildren<HullDamage>(true);
            if (crack != null)
            {
                if (crack.gameObject != prefab)
                {
                    prefab.SetActive(true);
                }
                crack.gameObject.SetActive(false);
                crack.OnCrackRepaired += OnHullRepaired;
                _pool.Add(crack);
            }
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
    
    private void Update()
    {
        if (forceHullDamageTrigger)
        {
            forceHullDamageTrigger = false;
            TrySpawnCrack();
        }
    }

    [ContextMenu("Start Hull Grace Period")]
    private void OnStartGracePeriod()
    {
        StartCoroutine(StartHullGracePeriod());
    }

    [ContextMenu("Force Spawn Crack")]
    private void DebugSpawnCrack()
    {
        if (Application.isPlaying) TrySpawnCrack();
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
    
    
    public int SpawnImmediateDamage(int amount)
    {
        int spawned = 0;
        for (int i = 0; i < amount; i++)
        {
            if (!TrySpawnCrack()) break;
            spawned++;
        }

        return spawned;
    }

    private bool TrySpawnCrack()
    {
        var available = _pool.FindAll(c => c != null && !c.gameObject.activeSelf);

        if (available.Count == 0)
        {
            StopSpawningBehaviour();
            return false;
        }

        var crack = available[Random.Range(0, available.Count)];
        crack.gameObject.SetActive(true);

     
        ActiveCrackCount = _pool.FindAll(c => c != null && c.gameObject.activeSelf).Count;
        Log.Info($"[HullDamageManager] Grieta generada. Grietas activas: {ActiveCrackCount}");

        GameEventChannel<OnHullPropertyChange>.RaiseEvent(new OnHullPropertyChange(hullDamageGOs.Count, ActiveCrackCount));
        return true;
    }

    private void OnHullRepaired(HullDamage hullDamage)
    {
        
        if (hullDamage != null)
        {
            hullDamage.gameObject.SetActive(false);
        }

        ActiveCrackCount = _pool.FindAll(c => c != null && c.gameObject.activeSelf).Count;
        Log.Info($"[HullDamageManager] Grieta reparada. Grietas restantes: {ActiveCrackCount}");

        GameEventChannel<OnHullPropertyChange>.RaiseEvent(new OnHullPropertyChange(hullDamageGOs.Count, ActiveCrackCount));

        if (ActiveCrackCount == 0)
        {
            StopSpawningBehaviour();
        }
    }
}