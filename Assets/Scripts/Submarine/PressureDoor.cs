using System.Collections;
using UnityEngine;

public class PressureDoor : MonoBehaviour, IInteractable
{
    [Header("Configuración de la Puerta")]
    [SerializeField] private float _openAngle = 90f;
    [SerializeField] private float _openSpeed = 5f;
    
    private bool _isOpen = false;
    private Coroutine _animationCoroutine;
    private Quaternion _closedRotation;
    private Quaternion _openRotation;

    private void Start()
    {
        _closedRotation = transform.rotation;
        _openRotation = _closedRotation * Quaternion.Euler(0, _openAngle, 0);
    }
    public void Interact(PlayerCharacter player)
    {
        _isOpen = !_isOpen;
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
        }
        _animationCoroutine = StartCoroutine(AnimateDoor(_isOpen ? _openRotation : _closedRotation));
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