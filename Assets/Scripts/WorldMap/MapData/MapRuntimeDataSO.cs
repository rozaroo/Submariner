using UnityEngine;

[CreateAssetMenu(menuName = "Map Properties/RuntimeData/MapScaleData")]
public class MapRuntimeDataSO : ScriptableObject
{
    public float worldMapSize;
    public float uiMapSize;
}