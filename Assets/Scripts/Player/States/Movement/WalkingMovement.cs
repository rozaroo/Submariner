using UnityEngine;

public class WalkingMovement : IMovementStrategy
{
    private float _moveVelocityY;
    private float _gravity = 9.81f;
    public void Move(MovementContext ctx)
    {
        Vector2 input = ctx.MovementAction.ReadValue<Vector2>();
        
        Vector3 move = ctx.Transform.right * input.x + 
                       ctx.Transform.forward * input.y;
        if (ctx.CharacterController.isGrounded && _moveVelocityY < 0) _moveVelocityY = -2f;
        _moveVelocityY += -_gravity * Time.deltaTime;
        move.y = _moveVelocityY;
        ctx.CharacterController.Move(move * ctx.MoveSpeed * Time.deltaTime);
    }
}
