using UnityEngine;
using UnityEngine.UI;

public class SonarVisualBehaviour : MonoBehaviour, ISetup, IResettable, IWorldElementBinder
{
    private MapRuntimeDataSO _mapRuntimeData;
    private float _worldOuterRadius;
    private float _worldInnerRadius;
    private Sprite _sonarIconSprite;
    private Color _mainSonarColor;
    private Color _secondarySonarColor;

    private GameObject _outerSonarGo;
    private GameObject _innerSonarGo;
    public bool IsInitialized { get; private set; }

    public void Setup() { }

    public void Setup(Sprite icon, Color mainCol, Color secCol, MapRuntimeDataSO runtimeData)
    {
        if (IsInitialized) return;
        IsInitialized = true;
        
        _sonarIconSprite = icon;
        _mainSonarColor = mainCol;
        _secondarySonarColor = secCol;
        _mapRuntimeData = runtimeData;
    }
    
    public void Bind(IWorldMapUIElement worldElement)
    {
        var worldMono = worldElement as MonoBehaviour;
        if (worldMono == null) return;
        
        var sonarProvider = worldMono.GetComponent<ISonarProvider>();
        if (sonarProvider != null)
        {
            _worldOuterRadius = sonarProvider.OuterRadius;
            _worldInnerRadius = sonarProvider.InnerRadius;

            ApplyScale();
        }
        else
        {
            Log.Warning($"[{name}] - Script 3D doesnt include ISonarProvider. Deactivating Sonar Visual.");
            ResetState();
        }
    }
    
    private void ApplyScale()
    {
        if (_mapRuntimeData == null || _mapRuntimeData.worldMapSize <= 0) return;

        float uiOuterRadius = (_worldOuterRadius / _mapRuntimeData.worldMapSize) * _mapRuntimeData.uiMapSize;
        float uiInnerRadius = (_worldInnerRadius / _mapRuntimeData.worldMapSize) * _mapRuntimeData.uiMapSize;
        
        if (_outerSonarGo == null)
        {
            _outerSonarGo = GenerateSonarRadius("OuterRadiusSonar", _mainSonarColor, _sonarIconSprite, uiOuterRadius);
            _innerSonarGo = GenerateSonarRadius("InnerRadiusSonar", _secondarySonarColor, _sonarIconSprite, uiInnerRadius);
            
            _outerSonarGo.transform.SetAsFirstSibling();
            _innerSonarGo.transform.SetAsFirstSibling();
        }
        else
        {
            _outerSonarGo.SetActive(true);
            _innerSonarGo.SetActive(true);
            _outerSonarGo.GetComponent<RectTransform>().sizeDelta = new Vector2(uiOuterRadius * 2, uiOuterRadius * 2);
            _innerSonarGo.GetComponent<RectTransform>().sizeDelta = new Vector2(uiInnerRadius * 2, uiInnerRadius * 2);
        }
    }
    
    private GameObject GenerateSonarRadius(string goName, Color color, Sprite sprite, float diameter)
    {
        GameObject go = new GameObject(goName);
        go.transform.SetParent(transform, false);
        
        Image image = go.AddComponent<Image>();
        image.raycastTarget = false;
        image.sprite = sprite;
        image.color = color;
        
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(diameter * 2, diameter * 2);
        return go;
    }

    public void ResetState()
    {
        if (_outerSonarGo != null) _outerSonarGo.SetActive(false);
        if (_innerSonarGo != null) _innerSonarGo.SetActive(false);
    }
}