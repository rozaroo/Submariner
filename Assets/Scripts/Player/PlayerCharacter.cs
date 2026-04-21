using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerCharacter : MonoBehaviour
{
    [Header("Control Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private string _moveActionName = "Move";
    [SerializeField] private string _interactionActionName = "Interact";
    
    [Header("References Settings")] 
    [SerializeField] CameraController _cameraController;
    private PlayerInput _playerInput;
    private CharacterController _controller;
    
    [Header("Movement Settings")]
    private Vector2 _moveDirectionInput;
    private float _moveVelocityY;
    private float _gravity = -9.81f;
    
    [Header("Interaction Settings (Raycast)")]
    [SerializeField] private Camera _playerCamera; //INTERACTION ONLY
    [SerializeField] private float _interactionDistance = 2.5f;
    [SerializeField] private LayerMask _interactableLayer;
    
    private void Start()
    {
        _playerInput = GetComponent<PlayerInput>();
        _controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (_playerInput.actions[_interactionActionName].WasPressedThisFrame())
        {
            TryInteractRaycast();
        }
        _moveDirectionInput = _playerInput.actions[_moveActionName].ReadValue<Vector2>();
        Move();
    }
    
    private void TryInteractRaycast()
    {
        Ray ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);
        
        if (Physics.Raycast(ray, out RaycastHit hit, _interactionDistance, _interactableLayer))
        {
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green, 2f);
            
            if (hit.collider.TryGetComponent(out IInteractable interactableObject))
            {
                interactableObject.Interact(this);
            }
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * _interactionDistance, Color.red, 2f);
        }
    }
    
    private void Move()
    {
        Vector3 move = transform.right * _moveDirectionInput.x + transform.forward * _moveDirectionInput.y;
        if (_controller.isGrounded && _moveVelocityY < 0) _moveVelocityY = -2f;
        _moveVelocityY += _gravity * Time.deltaTime;
        move.y = _moveVelocityY;
        _controller.Move(move * moveSpeed * Time.deltaTime);
    }
}
