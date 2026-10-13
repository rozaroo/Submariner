using UnityEngine;

public class PlayerGameplayFreeState : PlayerGameplayState
{
    private readonly PlayerCharacter _context;
    
    private float _walkBobFrequency = 3f;
    private float _walkBobAmplitude = 0.05f;
    
    private float _idleBobFrequency = 1f;
    private float _idleBobAmplitude = 0.015f; 
    
    private float _bobSmoothing = 5f;

    private float _bobTimer;
    private float _currentAmplitude;
    private float _currentFrequency;

    public PlayerGameplayFreeState(StateMachine sm, PlayerCharacter context) : base(sm)
    {
        _currentAmplitude = _idleBobAmplitude;
        _currentFrequency = _idleBobFrequency;
        _context = context;
    }

    public override void OnEnter()
    {
        _context.SetMovementStrategy(new WalkingMovement());
        _context.CamController.SetCameraStrategy(new NormalCameraStrategy());
        _context.FootstepSystem.SetActive(true);
        _context.SetMouseConfiguration(CursorLockMode.Locked,false);
        _context.EnableGameplayInputs();
    }

    public override void Update()
    {
        ApplyHeadbobbing();
    }

    private void ApplyHeadbobbing()
    {
        Vector3 velocity = _context.CharacterController.velocity; 
        Vector3 flatVelocity = new Vector3(velocity.x, 0f, velocity.z);
        float speed = flatVelocity.magnitude;

        bool isMoving = speed > 0.1f;
        
        float targetAmplitude = isMoving ? _walkBobAmplitude : _idleBobAmplitude;
        
        float targetFrequency = isMoving ? (_walkBobFrequency * speed) : _idleBobFrequency;
        
        _currentAmplitude = Mathf.Lerp(_currentAmplitude, targetAmplitude, Time.deltaTime * _bobSmoothing);
        _currentFrequency = Mathf.Lerp(_currentFrequency, targetFrequency, Time.deltaTime * _bobSmoothing);
        
        if (_context.CharacterController.isGrounded)
        {
            _bobTimer += Time.deltaTime * _currentFrequency;
            
            float bobOffsetY = Mathf.Sin(_bobTimer) * _currentAmplitude;
            float bobOffsetX = Mathf.Cos(_bobTimer / 2f) * (_currentAmplitude / 2f);
            
            _context.CamController.MainCamera.transform.localPosition = 
                _context.DefaultCameraLocalPosition + new Vector3(bobOffsetX, bobOffsetY, 0f);
        }
        else
        {
            _bobTimer = 0f;
            _context.CamController.MainCamera.transform.localPosition = Vector3.Lerp(
                _context.CamController.MainCamera.transform.localPosition, 
                _context.DefaultCameraLocalPosition, 
                Time.deltaTime * _bobSmoothing);
        }
    }

    public override void OnExit()
    {
        _context.DisableGameplayInputs();
    }
}