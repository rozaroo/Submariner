using UnityEngine;

public abstract class WorldMapElementSO : ScriptableObject
{
    [SerializeField] protected string elementName;
    [SerializeField] protected float requiredSize;
    [SerializeField] protected MapAssetSO mapAsset;
    
    public float RequiredSize => requiredSize;
    public abstract GameObject CreateElement();
}