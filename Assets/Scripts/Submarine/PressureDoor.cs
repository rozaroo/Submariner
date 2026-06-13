using System;
using System.Collections;
using UnityEngine;

public class PressureDoor : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    [SerializeField] private Vector3 _openAngle = new Vector3(0, 90f, 0);
    [SerializeField] private float _openSpeed = 5f;
    [SerializeField] private Transform _doorTransform;
    
    private bool _isOpen = false;
    private Coroutine _animationCoroutine;
    private Quaternion _closedRotation;
    private Quaternion _openRotation;
    private Vector3 _hingePosition;
    
    public event Action OnDoorOpen;
    public event Action OnDoorClose;
    public bool IsOpen => _isOpen;

    private void Start()
    {
        _closedRotation = _doorTransform.rotation;
        _hingePosition = transform.forward;
    }
    public void Interact(PlayerCharacter player)
    {
        _isOpen = !_isOpen;
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
        }
        
        if (_isOpen)
        {
            OpenDoor(player);
            OnDoorOpen?.Invoke();
        }
        else
        {
            CloseDoor();
            OnDoorClose?.Invoke();
        }
    }

    private void OpenDoor(PlayerCharacter player)
    {
        _isOpen = true;
        SFXManager.PostEvent("Start_Opening_Pressure_DoorSFX", gameObject);
        Vector3 direction = transform.position - player.transform.position;
        float dotProduct = Vector3.Dot(_hingePosition, direction);
        if (dotProduct < 0)
        {
            _openRotation = Quaternion.Euler(_openAngle) * _closedRotation;
        }
        else
        {
            _openRotation =  Quaternion.Euler(-_openAngle) * _closedRotation;
        }
        _animationCoroutine = StartCoroutine(AnimateDoor(_openRotation));
    }

    public void CloseDoor()
    {
        _isOpen = false;
        SFXManager.PostEvent("Start_Closing_Pressure_DoorSFX", gameObject);
        _animationCoroutine = StartCoroutine(AnimateDoor(_closedRotation));
    }

    private IEnumerator AnimateDoor(Quaternion targetRotation)
    {
        while (Quaternion.Angle(_doorTransform.rotation, targetRotation) > 0.01f)
        {
            float t = 1f - Mathf.Exp(-_openSpeed * Time.deltaTime);
            _doorTransform.rotation = Quaternion.Slerp(_doorTransform.rotation, targetRotation, t);
            yield return null;
        }
        _doorTransform.rotation = targetRotation;
    }
}