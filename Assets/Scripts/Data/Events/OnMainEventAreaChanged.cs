using UnityEngine;

public struct OnMainEventAreaChanged : IGameEvent
{
    public Vector3 WorldPosition;
    public float Radius;
    public bool IsActive;
    public string ObjectiveText;

    public OnMainEventAreaChanged(Vector3 worldPosition, float radius, bool isActive, string objectiveText)
    {
        WorldPosition = worldPosition;
        Radius = radius;
        IsActive = isActive;
        ObjectiveText = objectiveText;
    }
}