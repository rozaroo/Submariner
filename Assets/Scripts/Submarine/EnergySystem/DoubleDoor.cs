using System.Collections;
using UnityEngine;

public class DoubleDoor : MonoBehaviour, IInteractable
{
    [Header("Double Door Settings")]
    [SerializeField] private Transform firstDoor;
    [SerializeField] private Transform secondDoor;
    [SerializeField] private float _openAngle = 90f;
    [SerializeField] private float _openSpeed = 5f;
    
    private bool _isOpen = false;
    private Coroutine _animationCoroutine;
    
    private Quaternion _firstDoorClosedRotation;
    private Quaternion _secondDoorClosedRotation;
    
    private Quaternion _firstDoorOpenRotation;
    private Quaternion _secondDoorOpenRotation;

    private void Start()
    {
        _firstDoorClosedRotation  = firstDoor.rotation;
        _secondDoorClosedRotation = secondDoor.rotation;
        
        _firstDoorOpenRotation  = _firstDoorClosedRotation  * Quaternion.Euler(0,  _openAngle, 0);
        _secondDoorOpenRotation = _secondDoorClosedRotation * Quaternion.Euler(0, -_openAngle, 0);
    }
    
    public void Interact(PlayerCharacter player)
    {
        _isOpen = !_isOpen;
        if (_animationCoroutine != null)
            StopCoroutine(_animationCoroutine);

        _animationCoroutine = StartCoroutine(AnimateDoors(
            _isOpen ? _firstDoorOpenRotation  : _firstDoorClosedRotation,
            _isOpen ? _secondDoorOpenRotation : _secondDoorClosedRotation
        ));
    }

    private IEnumerator AnimateDoors(Quaternion firstTarget, Quaternion secondTarget)
    {
        while (Quaternion.Angle(firstDoor.rotation,  firstTarget)  > 0.01f ||
               Quaternion.Angle(secondDoor.rotation, secondTarget) > 0.01f)
        {
            float t = 1f - Mathf.Exp(-_openSpeed * Time.deltaTime);
            firstDoor.rotation  = Quaternion.Slerp(firstDoor.rotation,  firstTarget,  t);
            secondDoor.rotation = Quaternion.Slerp(secondDoor.rotation, secondTarget, t);
            yield return null;
        }

        firstDoor.rotation  = firstTarget;
        secondDoor.rotation = secondTarget;
    }
}
