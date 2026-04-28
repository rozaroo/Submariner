using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ButtonStation : MonoBehaviour, IStationControl
{
    [Header("Button Settings")]
    [SerializeField] private string colorParameter = "_Color";
    [SerializeField] private Color lockedColor = Color.gray;
    [SerializeField] private Color unlockedColor = Color.yellow;
    [SerializeField] private Color pressedColor = Color.greenYellow;
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private float transitionTime = 0.1f;
    
    public bool IsUnlocked { get; set; }
    public bool IsPressed { get; set; }
    public Action OnActivation { get; set; }
    
    private Renderer _renderer;
    private Coroutine _colorCoroutine;
    private bool _isPressed = false;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer == null)
        {
            Debug.LogError("Renderer component not found on ButtonStation.");
        }
        Lock();
    }

    #region ButtonLogic

    public void Lock()
    {
        IsUnlocked = false;
        ChangeColor(lockedColor);
    }
    public void Unlock()
    {
        IsUnlocked = true;
        ChangeColor(unlockedColor);
    }
    
    public void OnActionDown()
    {
        if (_isPressed) return;
        _isPressed = true;
        ChangeColor(pressedColor);
        OnActivation?.Invoke();
    }
    public void OnActionDrag(float deltaY) { }

    public void OnActionUp()
    {
        ChangeColor(activeColor);
    }
    public void RestartButton()
    {
        ChangeColor(lockedColor);
        IsUnlocked = false;
        _isPressed = false;
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
        _colorCoroutine = null;
    }

    #endregion
}