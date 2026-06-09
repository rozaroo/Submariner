using UnityEngine;

public class FootStepSystem : MonoBehaviour
{
    [SerializeField] private float minSpeedClamp = 1f;
    [SerializeField] private float maxSpeedClamp = 3f;
    [SerializeField] private float _velocityThreshold = 0.4f;
    [SerializeField] private float _maxVelocityThreshold = 0.2f;
    
    private CharacterController _characterController;
    private float _stepTimer;
    
    private void Update()
    {
        if (_characterController == null) return;
        
        if (!_characterController.isGrounded) return;

        float speed = new Vector3(_characterController.velocity.x, 0, _characterController.velocity.z).magnitude;
        if (speed < _maxVelocityThreshold) return;

        float currentInterval = _velocityThreshold / Mathf.Clamp(speed, minSpeedClamp, maxSpeedClamp);
        
        _stepTimer += Time.deltaTime;
        if (_stepTimer >= currentInterval)
        {
            _stepTimer = 0;
            PlayFootsteps();
        }
    }

    private void PlayFootsteps()
    {
        SFXManager.PostEvent("Start_LeverPullFinished", gameObject);
    }
}
