using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class WorldMapCollisionElement : WorldMapElement
{
    public event Action OnCollisionSubmarine; //NOTE: This is only if the GO requires other effects attached to it. Remove if unnecessary in the future.

    private void OnCollisionEnter(Collision other)
    {
        OnCollisionSubmarine?.Invoke();
        Destroy(gameObject);
    }
}
