using UnityEngine;

public readonly struct OnObstacleDamagedHull : IGameEvent
{
    public int ObstacleId { get; }
    public Vector3 Position { get; }

    public OnObstacleDamagedHull(Collider obstacle)
    {
        ObstacleId = obstacle.gameObject.GetInstanceID();
        Position = obstacle.bounds.center;
    }
}
