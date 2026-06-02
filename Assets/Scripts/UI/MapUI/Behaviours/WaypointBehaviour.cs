using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class WaypointBehaviour : MonoBehaviour, IPointerClickHandler, ISetup, IResettable
{
    public event Action OnRightClicked;
    private Action _onRightClickedAction;
    
    private TextMeshProUGUI _label;
    public bool IsInitialized { get; private set; }
    public LineBehaviour LineComp { get; set; }

    private void OnDisable()
    {
        if (_onRightClickedAction != null)
            OnRightClicked -= _onRightClickedAction;
    }
    
    public void Setup()
    {
        if (IsInitialized) return;
        IsInitialized = true;
        var modifiableText = GetComponentInChildren<ModifiableTextBehaviour>();
        if (modifiableText == null)
        {
            modifiableText = gameObject.AddComponent<ModifiableTextBehaviour>();
            modifiableText.Setup();
        }
        _label = gameObject.GetComponentInChildren<TextMeshProUGUI>();
    }
    
    public void SetIndex(int index)
    {
        if (_label != null)
            _label.text = index.ToString();
    }

    public void SetAction(Action action)
    {
        OnRightClicked += action;
        _onRightClickedAction = action;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            OnRightClicked?.Invoke();
    }

    public void ResetState()
    {
        OnRightClicked = null;
        _onRightClickedAction = null;
        
        if (_label != null)
            _label.text = string.Empty;
    }
}