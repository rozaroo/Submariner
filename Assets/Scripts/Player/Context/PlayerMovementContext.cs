using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementContext
{
    public Transform PlayerTransform { get; private set; }
    public float MoveSpeed { get; private set; }
    public InputAction MoveAction { get; private set; }
    public CharacterController Controller { get; private set; }
    public PlayerInput PlayerInput { get; private set; }

    public PlayerMovementContext(Transform playerTransform, float moveSpeed, InputAction moveAction, 
        CharacterController controller, PlayerInput playerInput)
    {
        PlayerTransform = playerTransform;
        MoveSpeed = moveSpeed;
        MoveAction = moveAction;
        Controller = controller;
        PlayerInput = playerInput;
    }
}
