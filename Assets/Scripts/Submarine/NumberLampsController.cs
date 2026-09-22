using System;
using System.Collections.Generic;
using UnityEngine;

public class NumberLampsController : MonoBehaviour
{
    [Header("Settings")]
    [Header("Configuración de Arquitectura")]
    [SerializeField] private List<NumberLampObject> lamps;
    [SerializeField] private int numberToShow = 0;

    private void Awake()
    {
        foreach (var lamp in lamps)
        {
            lamp.Initialize();
        }
    }

    private void Start()
    {
        UpdateDisplay(numberToShow);
    }

    public void UpdateDisplay(int value)
    {
        if(lamps == null || lamps.Count == 0)
        {
            Log.Warning("No lamps assigned to NumberLampsController.");
            return;
        }
        
        int maxDisplayableValue = (int)Mathf.Pow(10, lamps.Count) - 1;
        
        int clampedValue = Mathf.Clamp(value, 0, maxDisplayableValue);
        
        string formatString = "D" + lamps.Count.ToString();
        string valueAsString = clampedValue.ToString(formatString);
        
        for (int i = 0; i < lamps.Count; i++)
        {
            int digitToDisplay = valueAsString[i] - '0';
            
            lamps[i].ChangeNumber(digitToDisplay);
        }
    }
}
