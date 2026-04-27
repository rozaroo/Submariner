using System;
using UnityEngine;

public class Crack : MonoBehaviour
{
    [Header("Repair")]
    [SerializeField] private float repairDuration = 3f;

    // El CrackManager se suscribe para saber cuándo se cerró esta grieta
    public Action OnRepaired;

    private float _progress;

    // Llamado cada frame por Blowtorch mientras se mantiene click sobre esta grieta
    public void Repair(float amount)
    {
        _progress += amount;
        if (_progress >= repairDuration)
            Close();
    }

    private void Close()
    {
        Debug.Log($"[Crack] {gameObject.name} reparada.");
        OnRepaired?.Invoke();
        gameObject.SetActive(false);
    }

    // Al reactivarse resetea el progreso para poder reutilizarse
    private void OnEnable() => _progress = 0f;
}
