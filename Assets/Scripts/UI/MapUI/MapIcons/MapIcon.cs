using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(Image))]
public class MapIcon : MonoBehaviour, ISetup
{
    [SerializeField] private MapAssetSO mapAssetConfig;
    [SerializeField] private RectTransform iconRectTransform;
    [SerializeField] private Image image;
    public bool IsInitialized { get; private set; }

    public MapAssetSO MapAssetConfig {get => mapAssetConfig; set => mapAssetConfig = value; }

    private void Awake()
    {
        iconRectTransform = GetComponent<RectTransform>();
        image             = GetComponent<Image>();
    }
    
    public void Setup()
    {
        if (IsInitialized) return;
        IsInitialized = true;
        iconRectTransform = GetComponent<RectTransform>();
        image             = GetComponent<Image>();
        ApplyConfig();
        ApplyBehaviours();
    }
    
    [ContextMenu("MapIcon/ApplyConfig")]
    private void ApplyConfig()
    {
        image.sprite       = mapAssetConfig.sprite;
        image.color        = mapAssetConfig.tintColor;
        iconRectTransform.sizeDelta = mapAssetConfig.baseSize;
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