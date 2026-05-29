using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Map/Icon/MapAsset")]
public class MapAssetSO : ScriptableObject
{
    [Header("Visual Properties")] 
    public Sprite sprite;
    public Material material;
    public Color tintColor = Color.white;
    public Vector2 baseSize = Vector2.one;
    public float rotationOffset;
    
    [Header("Behaviour Properties")]
    public List<IconBehaviourSO> iconBehaviours;
    
}
