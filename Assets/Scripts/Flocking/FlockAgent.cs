using UnityEngine;

public class FlockAgent : MonoBehaviour
{
    private Vector3 _velocity;
    private Transform _selfTransform;

    public Vector3 Velocity => _velocity;

    private void Awake()
    {
        _selfTransform = transform;
    }

    public void Initialize(Vector3 initialVelocity)
    {
        _velocity = initialVelocity;
    }
    
    public void Move(Vector3 acceleration, FlockingSettingsSO settings)
    {
        _velocity += acceleration * Time.deltaTime;
        
        float speed = _velocity.magnitude;
        if (speed > settings.maxSpeed)
        {
            _velocity = _velocity.normalized * settings.maxSpeed;
        }
        else if (speed < settings.minSpeed && speed > 0)
        {
            _velocity = _velocity.normalized * settings.minSpeed;
        }

        _selfTransform.position += _velocity * Time.deltaTime;
        
        if (_velocity.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_velocity);
            _selfTransform.rotation = Quaternion.Slerp(_selfTransform.rotation, targetRotation, settings.rotationSpeed * Time.deltaTime);
        }
    }
}