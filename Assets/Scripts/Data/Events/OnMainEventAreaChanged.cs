using UnityEngine;

public struct OnMainEventAreaChanged : IGameEvent
{
    public Vector3 WorldPosition;
    public float Radius;
    public bool IsActive;

    public OnMainEventAreaChanged(Vector3 worldPosition, float radius, bool isActive)
    {
        WorldPosition = worldPosition;
        Radius = radius;
        IsActive = isActive;
    }
}