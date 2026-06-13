using System;
using System.Collections;
using UnityEngine;

public class AutomaticDoorClose : MonoBehaviour
{
    [Header("Automatic Door Settings")] 
    [SerializeField] private PressureDoor pressureDoor;
    [SerializeField] private float automaticDoorCloseTime = 3f;

    private bool _playerInsideDoorArea;
    private Coroutine _automaticCloseCoroutine;

    private void OnEnable()
    {
        if (pressureDoor == null) return;
        pressureDoor.OnDoorOpen += StartAutomaticDoorCloseTimer;
        pressureDoor.OnDoorClose += StopAutomaticDoorCloseTimer;
    }

    private void OnDisable()
    {
        if (pressureDoor == null) return;
        pressureDoor.OnDoorOpen -= StartAutomaticDoorCloseTimer;
        pressureDoor.OnDoorClose -= StopAutomaticDoorCloseTimer;
    }
    
    private void StartAutomaticDoorCloseTimer()
    {
        if (_automaticCloseCoroutine != null)
        {
            StopCoroutine(_automaticCloseCoroutine);
        }

        if (!_playerInsideDoorArea)
        {
            _automaticCloseCoroutine = StartCoroutine(AutomaticDoorClosing());
        }
    }

    private void StopAutomaticDoorCloseTimer()
    {
        if (_automaticCloseCoroutine != null)
        {
            StopCoroutine(_automaticCloseCoroutine);
        }
    }
    
    private IEnumerator AutomaticDoorClosing()
    {
        float elapsedTime = 0f;
        while (elapsedTime < automaticDoorCloseTime)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        pressureDoor.CloseDoor();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            _playerInsideDoorArea = true;
            if (!pressureDoor.IsOpen) return;
            
            StopAutomaticDoorCloseTimer();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            _playerInsideDoorArea = false;
            if (!pressureDoor.IsOpen) return;
            
            StartAutomaticDoorCloseTimer();
        }
    }
}
