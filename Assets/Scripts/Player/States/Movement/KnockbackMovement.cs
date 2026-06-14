using UnityEngine;
 
public class KnockbackMovement : IMovementStrategy
{
    private Vector3 _currentVelocity;
    private readonly float _gravity = -15f;
    private readonly float _drag;
 
    private float _timeActive;
    private readonly float _accelerationTime = 0.25f;
 
    private readonly Vector3 _targetVelocity;
 
    public float CurrentSpeedY => _currentVelocity.y;
 
    public KnockbackMovement(Vector3 direction, float force, float drag = 5f, float accelerationTime = 0.25f)
    {
        Vector3 horizontal = new Vector3(direction.x, 0, direction.z).normalized;
        _targetVelocity = horizontal * force;
        _drag = drag;
        _accelerationTime = accelerationTime;
        _currentVelocity   = _targetVelocity * 0.3f;
        _currentVelocity.y = 8f;
    }
 
    public void Move(MovementContext ctx)
    {
        _timeActive += Time.deltaTime;
 
        if (_timeActive <= _accelerationTime)
        {
            float t = _timeActive / _accelerationTime;
            float easeOut = Mathf.Sin(t * Mathf.PI * 0.5f);
            
            _currentVelocity.x = Mathf.Lerp(_targetVelocity.x * 0.3f, _targetVelocity.x, easeOut);
            _currentVelocity.z = Mathf.Lerp(_targetVelocity.z * 0.3f, _targetVelocity.z, easeOut);
        }
        else
        {
            Vector3 horizontalVel = new Vector3(_currentVelocity.x, 0f, _currentVelocity.z);
            horizontalVel = Vector3.Lerp(horizontalVel, Vector3.zero, _drag * Time.deltaTime);
            _currentVelocity.x = horizontalVel.x;
            _currentVelocity.z = horizontalVel.z;
        }
        
        if (ctx.CharacterController.isGrounded && _currentVelocity.y <= 0f) 
            _currentVelocity.y = -2f;
        else
            _currentVelocity.y += _gravity * Time.deltaTime;
        
        ctx.CharacterController.Move(_currentVelocity * Time.deltaTime);
    }
 
    public bool IsKnockbackFinished()
    {
        Vector3 horizontalVelocity = new Vector3(_currentVelocity.x, 0f, _currentVelocity.z);
        return _timeActive > _accelerationTime && horizontalVelocity.sqrMagnitude < 0.1f;
    }
}