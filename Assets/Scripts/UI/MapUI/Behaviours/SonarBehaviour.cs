using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*Radio de Mapa 2D (Amarillo): Es más grande que el Radio 3D, es capaz de detectar los “Puntos de Eventos Dinamicos”.
Radio de Mapa 3D (Verde): Una vez que algún “Punto de Estructura”, “Punto de Evento Dinámico” y/o “Punto de Interés” entre dentro de este Radio, estos serán visibles dentro del Radio de la Cámara de Periscopio.*/

//-Recibir Lista de MapIcons del MapManager, filtrar por tipo de icono y mostrar solo los que estén dentro del radio de mapa 2D (amarillo)
//-Si un icono entra dentro del radio de mapa 3D (verde), mostrarlo en el periscopio, si sale del radio de mapa 3D (verde), ocultarlo del periscopio pero seguir mostrándolo
//en el mapa 2D (amarillo) si sigue dentro del radio de mapa 2D (amarillo)
//-Recibir la posición del submarino, y mostrarlo en el mapa 2D (amarillo) y en el periscopio si está dentro del radio de mapa 3D (verde)
public class SonarBehaviour : MonoBehaviour, ISetup
{
    [SerializeField] private float _generalRadius = 50f;
    [SerializeField] private float _timePerSonarCheck = 0.2f;
    [SerializeField] private Sprite _sonarIconSprite;
    [SerializeField] private Color _mainSonarColor = Color.yellow;
    [SerializeField] private Color _secondarySonarColor = Color.green;
    
    private MapIcon _iconOwner;
    private Coroutine _sonarDistanceCoroutine;
    private List<MapIcon> _mapEventList;
    
    public bool IsInitialized { get; private set; }
    public List<MapIcon> MapEventList
    {
        set
        {
            _mapEventList = value; 
            InitializeSonarBehaviour();
        }
    }
    
    public void Setup() => Setup(_generalRadius, _timePerSonarCheck, _mainSonarColor, _secondarySonarColor, _sonarIconSprite);
    public void Setup(float radius, float timeSonarCheck, Color mainColor, Color secondaryColor, Sprite sonarIcon = null)
    {
        if (IsInitialized) 
            return; 
        
        IsInitialized = true;
        _generalRadius = radius;
        _timePerSonarCheck = timeSonarCheck;
        _mainSonarColor = mainColor;
        _secondarySonarColor = secondaryColor;
        _sonarIconSprite = sonarIcon;
        
        _iconOwner = GetComponent<MapIcon>();
        if (_iconOwner == null)
        {
            Log.Warning($"[{name}] - No MapIcon component found. SonarBehaviour requires a MapIcon component to function properly.");
        }

        if (sonarIcon != null)
        {
            GenerateSonarRadius("OuterRadiusSonar", _mainSonarColor, _generalRadius*2, 1);
            GenerateSonarRadius("InnerRadiusSonar", _secondarySonarColor, _generalRadius, 0);
        }
        InitializeSonarBehaviour();
    }

    private void GenerateSonarRadius(string goName, Color color, float radius, int order)
    {
        GameObject go = new GameObject(goName);
        go.transform.SetParent(transform,false);
        go.transform.SetSiblingIndex(order);
        
        Image image = go.AddComponent<Image>();
        image.raycastTarget = false;
        
        image.sprite = _sonarIconSprite;
        image.color = color;
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(radius, radius);
    }

    #region PrincipalSonar

    private void InitializeSonarBehaviour()
    {
        if (_sonarDistanceCoroutine != null)
        {
            StopCoroutine(_sonarDistanceCoroutine);
        }
        _sonarDistanceCoroutine = StartCoroutine(CheckDistances());
    }

    private void StopSonarBehaviour()
    {
        if (_sonarDistanceCoroutine != null)
        {
            StopCoroutine(_sonarDistanceCoroutine);
        }
    }
    
    private IEnumerator CheckDistances()
    {
        while (_mapEventList != null && _iconOwner != null)
        {
            foreach (MapIcon icon in _mapEventList)
            {
                float iconSonarDistance = Vector2.Distance(
                    _iconOwner.IconRectTransform.anchoredPosition, 
                    icon.IconRectTransform.anchoredPosition);
                
                bool isWithinOuterRadius = iconSonarDistance <= _generalRadius && 
                                           iconSonarDistance >= _generalRadius/2;
                if (icon.IsVisible != isWithinOuterRadius)
                {
                    icon.IsVisible = isWithinOuterRadius;
                    Log.Info("Sonar Check: " + icon.name + " is now " + (isWithinOuterRadius ? "visible" : "invisible") + " on the map. Distance: " + iconSonarDistance);
                }
                
                bool isWithinInnerRadius = iconSonarDistance < _generalRadius/2;
                if (isWithinInnerRadius)
                {
                    
                }
            }
            yield return new WaitForSeconds(_timePerSonarCheck);
        }
    }

    #endregion
    
    #region Testing Only

        #if UNITY_EDITOR
        [ContextMenu("Start Sonar Behaviour")]
        public void StartSonarTesting() => InitializeSonarBehaviour();
        
        [ContextMenu("Start Sonar Behaviour")]
        public void StopSonarTesting() => StopSonarBehaviour();
        #endif

    #endregion
}
