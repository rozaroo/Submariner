using UnityEngine;

public class PlayerMovementState : IState
{
    private PlayerMovementContext _playerMovementContext;
    private Vector2 _moveDirectionInput;
    private float _moveVelocityY;
    private float _gravity = -9.81f;
    
    public PlayerMovementState(PlayerMovementContext playerMovementContext) => _playerMovementContext = playerMovementContext;
    public void OnEnter()
    {
        Log.Info("[PlayerMovementState] Enter");
    }

    public void Update()
    {
        _moveDirectionInput = _playerMovementContext.MoveAction.ReadValue<Vector2>();
        Movement();
    }

    public void LateUpdate() { }

    public void OnExit()
    {
        Log.Info("[PlayerMovementState] Exit");
    }
    
    private void Movement()
    {
        Vector3 move = _playerMovementContext.PlayerTransform.right * _moveDirectionInput.x + 
                       _playerMovementContext.PlayerTransform.forward * _moveDirectionInput.y;
        if (_playerMovementContext.Controller.isGrounded && _moveVelocityY < 0) _moveVelocityY = -2f;
        _moveVelocityY += _gravity * Time.deltaTime;
        move.y = _moveVelocityY;
        _playerMovementContext.Controller.Move(move * _playerMovementContext.MoveSpeed * Time.deltaTime);
    }
}
