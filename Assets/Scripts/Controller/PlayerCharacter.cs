using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerCharacter : MonoBehaviour, IPossessable
{
    [Header("Control Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Vector2 mouseSensitivity = new Vector2(1f, 1f);
    
    [Header("References Settings")] 
    private Transform _cameraTransform;
    private PlayerInput _playerInput;
    private CharacterController _controller;
    
    [Header("Movement Settings")]
    private Vector2 _moveDirectionInput;
    private float _pitch;
    private float _moveVelocityY;
    private float _gravity = -9.81f;
    
    private void Start()
    {
        _cameraTransform = Camera.main.transform;
        _playerInput = GetComponent<PlayerInput>();
        _controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        _moveDirectionInput = _playerInput.actions["Move"].ReadValue<Vector2>();
        Move();
    }
    
    private void Move()
    {
        Vector3 move = transform.right * _moveDirectionInput.x + transform.forward * _moveDirectionInput.y;
        if (_controller.isGrounded && _moveVelocityY < 0) _moveVelocityY = -2f;
        _moveVelocityY += _gravity * Time.deltaTime;
        move.y = _moveVelocityY;
        _controller.Move(move * moveSpeed * Time.deltaTime);
    }
    
    public void Look(InputAction.CallbackContext context)
    {
        Vector2 lookInput = context.ReadValue<Vector2>();
        float yaw = lookInput.x * mouseSensitivity.x;
        float pitch = lookInput.y * mouseSensitivity.y;
        
        transform.Rotate(Vector3.up * yaw);
        
        _pitch -= pitch;
        _pitch = Mathf.Clamp(_pitch, -90f, 90f);
        _cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    public void Possess()
    {
        enabled = true;
    }

    public void UnPossess()
    {
        enabled = false;
    }
}
