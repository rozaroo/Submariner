using System.Collections;
using UnityEngine;

public class PressureDoor : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    [SerializeField] private Vector3 _openAngle = new Vector3(0, 90f, 0);
    [SerializeField] private float _openSpeed = 5f;
    
    private bool _isOpen = false;
    private Coroutine _animationCoroutine;
    private Quaternion _closedRotation;
    private Quaternion _openRotation;

    private void Start()
    {
        _closedRotation = transform.rotation;
        _openRotation = _closedRotation * Quaternion.Euler(_openAngle);
    }
    public void Interact(PlayerCharacter player)
    {
        _isOpen = !_isOpen;
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
        }

        Quaternion rotation;
        if (_isOpen)
        {
            SFXManager.PostEvent("Start_Opening_Pressure_DoorSFX", gameObject);
            rotation = _openRotation;
        }
        else
        {
            SFXManager.PostEvent("Start_Closing_Pressure_DoorSFX", gameObject);
            rotation = _closedRotation;
        }
        
        _animationCoroutine = StartCoroutine(AnimateDoor(rotation));
    }

    private IEnumerator AnimateDoor(Quaternion targetRotation)
    {
        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.01f)
        {
            float t = 1f - Mathf.Exp(-_openSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
            yield return null;
        }
        transform.rotation = targetRotation;
    }
}