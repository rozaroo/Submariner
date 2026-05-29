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
    [SerializeField] private Sprite _sonarIconSprite;
    [SerializeField] private float _radius = 50f;
    [SerializeField] private float _timePerSonarCheck = 0.2f;
    [SerializeField] private Color _sonarColor = Color.cyan;
    
    private MapIcon _iconOwner;
    private GameObject _sonarGo;
    private RectTransform _sonarRect;
    private Image _sonarImage;
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
    
    public void Setup() => Setup(_radius, _timePerSonarCheck, _sonarColor);
    public void Setup(float radius, float timeSonarCheck, Color color, Sprite sonarIcon = null)
    {
        if (IsInitialized) 
            return; 
        
        IsInitialized = true;
        _radius = radius;
        _timePerSonarCheck = timeSonarCheck;
        _sonarColor = color;
        _sonarIconSprite = sonarIcon;
        
        _iconOwner = GetComponent<MapIcon>();
        if (_iconOwner == null)
        {
            Log.Warning($"[{name}] - No MapIcon component found. SonarBehaviour requires a MapIcon component to function properly.");
        }

        if (sonarIcon != null)
        {
            GameObject go = new GameObject("SonarRadio");
            go.transform.SetParent(transform,false);
            go.transform.SetSiblingIndex(0);
            
            _sonarImage = go.AddComponent<Image>();
            _sonarImage.raycastTarget = false;
            _sonarRect = go.GetComponent<RectTransform>();
            UpdateSonarVisuals();
        }
    }
    
    [ContextMenu("Update Sonar Visuals")]
    public void UpdateSonarVisuals()
    {
        _sonarImage.sprite = _sonarIconSprite;
        _sonarImage.color = _sonarColor;
        _sonarRect.sizeDelta = new Vector2(_radius*2, _radius*2);
    }
    
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
                bool isWithin2DRadius = iconSonarDistance <= _radius;
                
                if (icon.IsVisible != isWithin2DRadius)
                {
                    icon.IsVisible = isWithin2DRadius;
                    Log.Info("Sonar Check: " + icon.name + " is now " + (isWithin2DRadius ? "visible" : "invisible") + " on the map. Distance: " + iconSonarDistance);
                }
            }
            yield return new WaitForSeconds(_timePerSonarCheck);
        }
    }
    
    #region Testing Only

        #if UNITY_EDITOR
        [ContextMenu("Start Sonar Behaviour")]
        public void StartSonarTesting() => InitializeSonarBehaviour();
        
        [ContextMenu("Start Sonar Behaviour")]
        public void StopSonarTesting() => StopSonarBehaviour();
        #endif

    #endregion
}
