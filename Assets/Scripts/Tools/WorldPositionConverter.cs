using UnityEngine;

public static class WorldPositionConverter
{
    public static Vector3 MapToWorld(Vector2 mapPosition, float worldSize, float mapSize)
    {
        float normalizedX = mapPosition.x / mapSize;
        float normalizedZ = mapPosition.y / mapSize;
        Vector3 result = new Vector3(normalizedX * worldSize, 0, normalizedZ * worldSize);
        return result;
    }

    public static Vector2 WorldToMap(Vector3 worldPosition, float worldSize, float mapSize)
    {
        float normalizedX = worldPosition.x / worldSize;
        float normalizedZ = worldPosition.z / worldSize;
        return new Vector2(normalizedX * mapSize, normalizedZ * mapSize);
    }
}
