using System;
using UnityEngine;

public class HullDamage : MonoBehaviour
{
    [Header("Repair Properties")]
    [SerializeField] private float repairDuration = 3f; 
    
    public event Action<HullDamage> OnCrackRepaired;
    private float _progress;
    
    public void Repair(float amount)
    {
        _progress += amount;
        if (_progress >= repairDuration)
            Close();
    }

    private void Close()
    {
        OnCrackRepaired?.Invoke(this);
        gameObject.SetActive(false);
    }
    
    private void OnEnable() => _progress = 0f;
}
