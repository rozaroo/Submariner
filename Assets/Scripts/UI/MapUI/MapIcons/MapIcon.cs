using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class MapIcon : MonoBehaviour, ISetup
{
    [SerializeField] private MapAssetSO mapAssetConfig;
    [SerializeField] private RectTransform iconRectTransform;
    [SerializeField] private RectTransform iconImageRectTransform;
    [SerializeField] private Image image;
    public bool IsInitialized { get; private set; }
    public bool IsVisible { get => gameObject.activeSelf; set => gameObject.SetActive(value); }
    
    public RectTransform IconRectTransform => iconRectTransform;
    public MapAssetSO MapAssetConfig {get => mapAssetConfig; set => mapAssetConfig = value; }

    private void Awake()
    {
        iconRectTransform = GetComponent<RectTransform>();
    }
    
    public void Setup()
    {
        if (IsInitialized) return;
        
        IsInitialized = true;
        iconRectTransform = GetComponent<RectTransform>();
        ApplyConfig();
        ApplyBehaviours();
    }
    
    [ContextMenu("MapIcon/ApplyConfig")]
    private void ApplyConfig()
    {
        GameObject go = new GameObject("BaseIcon");
        go.transform.SetParent(transform,false);
        image = go.AddComponent<Image>();
        iconImageRectTransform = go.GetComponent<RectTransform>();
        
        
        image.sprite       = mapAssetConfig.sprite;
        image.color        = mapAssetConfig.tintColor;
        iconImageRectTransform.localRotation = Quaternion.Euler(
            iconImageRectTransform.localEulerAngles.x,
            iconImageRectTransform.localEulerAngles.y,
            iconImageRectTransform.localEulerAngles.z + mapAssetConfig.rotationOffset);
        iconImageRectTransform.sizeDelta = mapAssetConfig.baseSize;
        
        iconRectTransform.sizeDelta = mapAssetConfig.baseSize; //Note: This is for Raycast, DONT remove.
        
        if (mapAssetConfig.material != null)
            image.material = mapAssetConfig.material;
    }
    
    [ContextMenu("MapIcon/ApplyBehaviours")]
    private void ApplyBehaviours()
    {
        if (mapAssetConfig.iconBehaviours.Count <= 0) return;
        foreach (var behaviour in mapAssetConfig.iconBehaviours)
            behaviour.ApplyComponent(gameObject);
    }
}