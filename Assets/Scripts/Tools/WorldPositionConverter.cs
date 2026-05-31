using UnityEngine;

public static class WorldPositionConverter
{
    public static Vector3 MapDeltaToWorldPosition(Vector2 target, Vector2 centerTarget, float scale)
    {
        Vector2 relativePos = target - centerTarget;
        return new Vector3(relativePos.x, 0, relativePos.y) * scale;
    }
}
