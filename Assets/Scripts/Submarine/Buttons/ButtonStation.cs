using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ButtonStation : MonoBehaviour, IButtonControls
{
    [Header("Button Settings")]
    [SerializeField] private string colorParameter = "_Color";
    [SerializeField] private Color lockedColor = Color.gray;
    [SerializeField] private Color unlockedColor = Color.yellow;
    [SerializeField] private Color pressedColor = Color.greenYellow;
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private float transitionTime = 0.1f;
    
    public bool isLocked { get; set; }
    public bool isPressed { get; set; }
    public bool isActive { get; set; }
    public Action onActivation { get; set; }
    public Action onDeactivation { get; set; }
    private Renderer _renderer;
    private Coroutine _colorCoroutine;
    private Light _readyLight;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer == null)
        {
            Log.Error("Renderer component not found on ButtonStation.");
        }
        Lock();
    }

    #region ButtonLogic

    public void Lock()
    {
        isLocked = true;
        ChangeColor(lockedColor);
    }
    public void Unlock() => Unlock(0f, 0f);

    internal void Unlock(float lightIntensity, float lightRange)
    {
        isLocked = false;
        ChangeColor(unlockedColor);

        if (lightIntensity <= 0f || lightRange <= 0f) return;

        if (_readyLight == null)
        {
            var lightObject = new GameObject("Button Ready Light");
            lightObject.transform.SetParent(transform, false);
            BoxCollider buttonCollider = GetComponent<BoxCollider>();
            lightObject.transform.localPosition = buttonCollider.center
                + Vector3.up * (buttonCollider.size.y * 0.5f + 0.045f);
            _readyLight = lightObject.AddComponent<Light>();
            _readyLight.type = LightType.Point;
            _readyLight.shadows = LightShadows.None;
        }

        _readyLight.color = unlockedColor;
        _readyLight.range = lightRange;
        _readyLight.intensity = lightIntensity;
        _readyLight.enabled = true;
    }

    public void SetActive(bool active)
    {
        ChangeColor(active ? activeColor : unlockedColor);
    }

    public void OnActionDown()
    {
        if (!isPressed && !isLocked)
        {
            isPressed = true;
            SFXManager.PostEvent("Start_ButtonPress", gameObject);
            ChangeColor(pressedColor);
            onActivation?.Invoke();
        }
    }

    public void OnActionUp() //TODO: Not Being used, change the usage on Drainage Station (or others) or remove if useless (Prefer the first option)
    {
        if (isPressed && !isLocked)
        {
            ChangeColor(activeColor);
        }
    }
    
    public void Restart()
    {
        Lock();
        isPressed = false;
    }
    #endregion

    #region ColorTransitionLogic

    private void ChangeColor(Color toColor)
    {
        if (_readyLight != null) _readyLight.enabled = false;
        if (_colorCoroutine != null)
        {
            StopCoroutine(_colorCoroutine);
        }
        _colorCoroutine = StartCoroutine(LerpColor(toColor));
    }
    
    private IEnumerator LerpColor(Color toColor)
    {
        Color fromColor = _renderer.material.GetColor(colorParameter);
        float elapsedtime = 0f;
        while (elapsedtime < transitionTime)
        {
            elapsedtime += Time.deltaTime;
            float t = elapsedtime / transitionTime;
            _renderer.material.SetColor(colorParameter, Color.Lerp(fromColor, toColor, t));            
            yield return null;
        }
        _renderer.material.SetColor(colorParameter, toColor);
    }
    
    #endregion
}
