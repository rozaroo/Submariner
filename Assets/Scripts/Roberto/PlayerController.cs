using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;
    public float gravity = -9.81f;
    public Transform playerBody;
    public Transform cameraTransform;
    public CharacterController controller;
    public InputActionReference moveAction;
    public InputActionReference lookAction;
    float xRotation = 0f;
    float yVelocity = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }
    void OnEnable()
    {
        moveAction.action.Enable();
        lookAction.action.Enable();
    }
    void OnDisable()
    {
        moveAction.action.Disable();
        lookAction.action.Disable();
    }
    void Update()
    {
        Move();
        Look();
    }
    void Move()
    {
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        Vector3 move = playerBody.forward * input.y + playerBody.right * input.x;
        if (controller.isGrounded && yVelocity < 0) yVelocity = -2f;
        yVelocity += gravity * Time.deltaTime;
        move.y = yVelocity;
        controller.Move(move * moveSpeed * Time.deltaTime);
    }
    void Look()
    {
        Vector2 mouseInput = lookAction.action.ReadValue<Vector2>();
        float mouseX = mouseInput.x * mouseSensitivity;
        float mouseY = mouseInput.y * mouseSensitivity;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
