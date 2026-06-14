using UnityEngine;

public struct OnSubmarineImpact : IGameEvent
{
    public Vector3 Normal;
    public float ImpactSpeed;

    public OnSubmarineImpact(Vector3 normal, float impactSpeed)
    {
        Normal = normal;
        ImpactSpeed = impactSpeed;
    }
}
