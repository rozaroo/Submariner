using System;
using UnityEngine;

[Serializable]
public struct OnSubmarineCollision : IGameEvent
{
    public Collider subCollider;
    public OnSubmarineCollision(Collider subCollider)
    {
        this.subCollider = subCollider;
    }
}
