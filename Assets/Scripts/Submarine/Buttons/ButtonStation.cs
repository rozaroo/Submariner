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
    public Action onActivation { get; set; }
    private Renderer _renderer;
    private Coroutine _colorCoroutine;

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
    public void Unlock()
    {
        isLocked = false;
        ChangeColor(unlockedColor);
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