using System;
using System.Collections.Generic;
using UnityEngine;

// Controla el spawn aleatorio de grietas en las zonas designadas y expone
// cuántas están activas para que otros sistemas (ej. FloodSystem) reaccionen.
public class CrackManager : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject crackPrefab;

    [Header("Zonas de spawn — asignar en el Inspector")]
    [SerializeField] private Transform[] spawnZones;

    [Header("Tiempo entre spawns")]
    [SerializeField] private float minSpawnInterval = 5f;
    [SerializeField] private float maxSpawnInterval = 15f;

    public int ActiveCrackCount { get; private set; }

    // Se dispara cada vez que aparece una grieta nueva
    public Action OnCrackSpawned;

    private readonly List<Crack> _pool = new List<Crack>();
    private float _nextSpawnTime;

    private void Start()
    {
        // Pre-instanciar una grieta por zona, todas desactivadas al inicio
        foreach (var zone in spawnZones)
        {
            var go = Instantiate(crackPrefab, zone.position, zone.rotation);
            go.SetActive(false);

            var crack = go.GetComponent<Crack>();
            crack.OnRepaired += () => OnCrackRepaired(crack);
            _pool.Add(crack);
        }

        ScheduleNextSpawn();
    }

    private void Update()
    {
        if (Time.time >= _nextSpawnTime)
            TrySpawnCrack();
    }

    private void TrySpawnCrack()
    {
        // Filtrar las grietas que aún no están activas
        var available = _pool.FindAll(c => !c.gameObject.activeSelf);
        if (available.Count == 0)
        {
            // Todas activas, reintentar más tarde
            ScheduleNextSpawn();
            return;
        }

        var crack = available[UnityEngine.Random.Range(0, available.Count)];
        crack.gameObject.SetActive(true);
        ActiveCrackCount++;

        OnCrackSpawned?.Invoke();
        Debug.Log($"[CrackManager] Grieta spawneada. Activas: {ActiveCrackCount}");
        ScheduleNextSpawn();
    }

    private void OnCrackRepaired(Crack crack)
    {
        ActiveCrackCount = Mathf.Max(0, ActiveCrackCount - 1);
        Debug.Log($"[CrackManager] Grieta reparada. Activas: {ActiveCrackCount}");
    }

    private void ScheduleNextSpawn() =>
        _nextSpawnTime = Time.time + UnityEngine.Random.Range(minSpawnInterval, maxSpawnInterval);
}
