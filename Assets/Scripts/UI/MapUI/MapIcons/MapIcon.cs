using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class MapIcon : MonoBehaviour, ISetup
{
    [SerializeField] private MapAssetSO mapAssetConfig;
    
    private RectTransform _iconRectTransform;
    private RectTransform _iconImageRectTransform;
    private Image _image;
    
    private IResettable[] _cachedResettable;
    private IWorldElementBinder[] _cachedBinders;
    
    public bool IsInitialized { get; private set; }
    public bool IsVisible { get => gameObject.activeSelf; set => gameObject.SetActive(value); }
    public RectTransform IconRectTransform => _iconRectTransform;
    public MapAssetSO MapAssetConfig { get => mapAssetConfig; set => mapAssetConfig = value; }

    private void Awake()
    {
        _iconRectTransform = GetComponent<RectTransform>();
    }
    
    public void Setup()
    {
        if (IsInitialized) return;
        IsInitialized = true;
        
        ApplyConfig();
        ApplyBehaviours();
        
        _cachedResettable = GetComponentsInChildren<IResettable>(true);
        _cachedBinders = GetComponentsInChildren<IWorldElementBinder>(true);
    }
    
    private void ApplyConfig()
    {
        GameObject go = new GameObject("BaseIcon");
        go.transform.SetParent(transform, false);
        _image = go.AddComponent<Image>();
        _iconImageRectTransform = go.GetComponent<RectTransform>();
        
        _image.sprite = mapAssetConfig.sprite;
        _image.color = mapAssetConfig.tintColor;
        _iconImageRectTransform.localRotation = Quaternion.Euler(0, 0, mapAssetConfig.rotationOffset);
        _iconImageRectTransform.sizeDelta = mapAssetConfig.baseSize;
        
        _iconRectTransform.sizeDelta = mapAssetConfig.baseSize; 
        
        if (mapAssetConfig.material != null)
            _image.material = mapAssetConfig.material;
    }
    
    private void ApplyBehaviours()
    {
        if (mapAssetConfig.iconBehaviours.Count <= 0) return;
        foreach (var behaviour in mapAssetConfig.iconBehaviours)
        {
            if(behaviour != null)
                behaviour.ApplyComponent(gameObject);
        }
    }
    
    public void BindToWorldEntity(IWorldMapUIElement element)
    {
        if (_cachedBinders == null) return;
        for (int i = 0; i < _cachedBinders.Length; i++)
        {
            if (_cachedBinders[i] != null) _cachedBinders[i].Bind(element);
        }
    }
    
    public void ResetToDefaultState()
    {
        if (!IsInitialized || mapAssetConfig == null) return;
        
        _image.color = mapAssetConfig.tintColor;
        _iconImageRectTransform.localRotation = Quaternion.Euler(0, 0, mapAssetConfig.rotationOffset);
        _iconImageRectTransform.sizeDelta = mapAssetConfig.baseSize;
        _iconRectTransform.sizeDelta = mapAssetConfig.baseSize;
        
        StopAllCoroutines();
        ResetBehaviours();
    }
    
    private void ResetBehaviours()
    {
        if (_cachedResettable == null) return;
        for (int i = 0; i < _cachedResettable.Length; i++)
        {
            if (_cachedResettable[i] != null) _cachedResettable[i].ResetState();
        }
    }
}