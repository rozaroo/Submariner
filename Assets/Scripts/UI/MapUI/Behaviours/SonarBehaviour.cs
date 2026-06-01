using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SonarBehaviour : MonoBehaviour, ISetup, IResettable
{
    [Header("Sonar Properties")]
    [SerializeField] private float generalRadius = 50f;
    [SerializeField] private float timePerSonarCheck = 0.2f;
    [SerializeField] private Sprite sonarIconSprite;
    [SerializeField] private Color mainSonarColor = Color.yellow;
    [SerializeField] private Color secondarySonarColor = Color.green;

    [Header("Event Channels")] 
    [SerializeField] private MapIconPropertyEventChannelSO onEventIconEnteredRadius;
    [SerializeField] private MapIconPropertyEventChannelSO onEventIconLeftRadius;
    
    private MapIcon _iconOwner;
    private Coroutine _sonarDistanceCoroutine;
    private List<MapIcon> _detectableIcons;
    
    public bool IsInitialized { get; private set; }
    public List<MapIcon> MapEventList
    {
        set
        {
            _detectableIcons = value; 
            InitializeSonarBehaviour();
        }
    }
    
    public void Setup() => Setup(generalRadius, timePerSonarCheck, mainSonarColor, secondarySonarColor, 
        sonarIconSprite, onEventIconEnteredRadius, onEventIconLeftRadius);
    
    public void Setup(float radius, float timeSonarCheck, Color mainColor, Color secondaryColor, 
        Sprite sonarIcon = null, MapIconPropertyEventChannelSO radiusEnteredEvent = null, MapIconPropertyEventChannelSO radiusLeftEvent = null)
    {
        if (IsInitialized) 
            return; 
        
        IsInitialized = true;
        generalRadius = radius;
        timePerSonarCheck = timeSonarCheck;
        mainSonarColor = mainColor;
        secondarySonarColor = secondaryColor;
        sonarIconSprite = sonarIcon;
        _iconOwner = GetComponent<MapIcon>();
        
        if (_iconOwner == null)
        {
            Log.Warning($"[{name}] - No MapIcon component found. SonarBehaviour requires a MapIcon component to function properly.");
        }
        if (sonarIcon != null)
        {
            GameObject outerSonar = GenerateSonarRadius("OuterRadiusSonar", mainSonarColor, generalRadius*2);
            GameObject innerSonar = GenerateSonarRadius("InnerRadiusSonar", secondarySonarColor, generalRadius);
            
            outerSonar.transform.SetSiblingIndex(0);
            innerSonar.transform.SetSiblingIndex(1);

        }
        
        if(radiusEnteredEvent != null && radiusLeftEvent != null)
        {
            onEventIconEnteredRadius = radiusEnteredEvent;
            onEventIconLeftRadius = radiusLeftEvent;
        }
        else
        {
            Log.Warning("SonarBehaviour: One or both of the event channels for radius entry/exit are not assigned. " +
                        "This may lead to missing notifications when icons enter or leave the sonar radius.");
        }
    }

    private GameObject GenerateSonarRadius(string goName, Color color, float radius)
    {
        GameObject go = new GameObject(goName);
        go.transform.SetParent(transform,false);
        
        Image image = go.AddComponent<Image>();
        image.raycastTarget = false;
        
        image.sprite = sonarIconSprite;
        image.color = color;
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(radius, radius);

        return go;
    }

    #region SonarBehaviour

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
        List<MapIcon> insideRadius = new List<MapIcon>();
        while (_detectableIcons != null && _iconOwner != null)
        {
            foreach (MapIcon icon in _detectableIcons)
            {
                float iconSonarDistance = Vector2.Distance(
                    _iconOwner.IconRectTransform.anchoredPosition, 
                    icon.IconRectTransform.anchoredPosition);
                
                bool isWithinOuterRadius = iconSonarDistance <= generalRadius && 
                                           iconSonarDistance >= generalRadius/2;
                if (icon.IsVisible != isWithinOuterRadius)
                {
                    icon.IsVisible = isWithinOuterRadius;
                    Log.Info("Sonar Check: " + icon.name + " is now " + (isWithinOuterRadius ? "visible" : "invisible") + " on the map. Distance: " + iconSonarDistance);
                }
                
                if (iconSonarDistance < generalRadius/2 && !insideRadius.Contains(icon))
                {
                    insideRadius.Add(icon);
                    onEventIconEnteredRadius?.RaiseEvent(icon);
                    Log.Info("Sonar Check: " + icon.name + " entered the inner radius. Distance: " + iconSonarDistance);
                }
                else if (iconSonarDistance > generalRadius/2 && insideRadius.Contains(icon))
                {
                    insideRadius.Remove(icon);
                    onEventIconLeftRadius?.RaiseEvent(icon);
                    Log.Info("Sonar Check: " + icon.name + " left the inner radius. Distance: " + iconSonarDistance);
                }
            }
            yield return new WaitForSeconds(timePerSonarCheck);
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

    public void ResetState()
    {
        StopSonarBehaviour();
        
        if (_detectableIcons != null)
        {
            _detectableIcons.Clear();
        }
    }
}
