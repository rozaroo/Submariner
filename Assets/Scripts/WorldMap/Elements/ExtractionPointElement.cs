using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ExtractionPointElement : WorldMapUIElement
{
    public event Action OnSubmarineReachedExtraction;
    
    private void OnTriggerEnter(Collider other) => TryReachExtraction(other);

    private void OnTriggerStay(Collider other) => TryReachExtraction(other);

    private void TryReachExtraction(Collider other)
    {
        Action reachedExtraction = OnSubmarineReachedExtraction;
        if (!isActiveAndEnabled || !IsInitialized || reachedExtraction == null) return;

        Rigidbody body = other.attachedRigidbody;
        if (body != null && body.TryGetComponent<SubmarineMovement>(out _))
            reachedExtraction.Invoke();
    }
}
