using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ExtractionPointElement : WorldMapUIElement
{
    public event Action OnSubmarineReachedExtraction;

    private void OnCollisionEnter(Collision other)
    {
        OnSubmarineReachedExtraction?.Invoke();
    }
}