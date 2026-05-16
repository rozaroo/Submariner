using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapAssetSO", menuName = "Scriptable Objects/MapAssetSO")]
public class MapAssetSO : ScriptableObject
{
    [Header("Visual Properties")] 
    public Sprite sprite;
    public Material material;
    public Color tintColor = Color.white;
    public Vector2 baseSize = Vector2.one;
    
    [Header("Behaviour Properties")]
    public List<IconBehaviourSO> iconBehaviours;
    
}
