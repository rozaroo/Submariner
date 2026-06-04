using UnityEngine;

public class PlayerMovementState : IState
{
    public PlayerMovementContext Context { get; set; }
    private Vector2 _moveDirectionInput;
    private float _moveVelocityY;
    private float _gravity = -9.81f;
    
    public PlayerMovementState(PlayerMovementContext context) => Context = context;
    public void OnEnter() { }

    public void Update()
    {
        _moveDirectionInput = Context.MoveAction.ReadValue<Vector2>();
        Movement();
    }

    public void LateUpdate() { }

    public void OnExit() { }
    
    private void Movement()
    {
        Vector3 move = Context.PlayerTransform.right * _moveDirectionInput.x + 
                       Context.PlayerTransform.forward * _moveDirectionInput.y;
        if (Context.Controller.isGrounded && _moveVelocityY < 0) _moveVelocityY = -2f;
        _moveVelocityY += _gravity * Time.deltaTime;
        move.y = _moveVelocityY;
        Context.Controller.Move(move * Context.MoveSpeed * Time.deltaTime);
    }
}
