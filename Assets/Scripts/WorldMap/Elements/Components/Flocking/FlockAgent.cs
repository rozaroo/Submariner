using UnityEngine;

public class FlockAgent : MonoBehaviour, IAnimateableAgent
{
    private Vector3 _velocity;
    private Transform _selfTransform;
    private Animator _animator;

    public Vector3 Velocity => _velocity;

    private void Awake()
    {
        _selfTransform = transform;
        EnsureAnimatorRef(); //Security check for animator reference was causing problems.
        RandomizeAnimationPlayback();
    }
    
    private void EnsureAnimatorRef()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
        }
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
            Quaternion lookRotation = Quaternion.LookRotation(_velocity);
            Quaternion rotationOffset = Quaternion.Euler(90f, 0f, 0f); //TODO: Change via other parameters.
            _selfTransform.rotation = lookRotation * rotationOffset;
        }
    }
    
    public void RandomizeAnimationPlayback()
    {
        if (_animator == null) return;
        
        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
        float randomOffset = Random.Range(0f, 1f);
        _animator.Play(state.fullPathHash, 0, randomOffset);
    }

    public void WakeUpAnimation()
    {
        if (_animator == null) return;
        _animator.enabled = true;
        _animator.speed = 1f; 
        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
        float randomOffset = Random.Range(0f, 1f);
        float randomSpeed = Random.Range(0.7f, 1.3f);
        _animator.SetFloat("AnimationSpeed", randomSpeed);
        _animator.PlayInFixedTime(state.fullPathHash, 0, randomOffset);
        _animator.Update(Time.deltaTime); 
    }
}