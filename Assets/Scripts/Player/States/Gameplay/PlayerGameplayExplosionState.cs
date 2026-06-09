using UnityEngine;
 
public class PlayerGameplayExplosionState : PlayerGameplayState
{
    private readonly PlayerCharacter _player;
    
    private readonly Vector3 _impactDir;
    private readonly float _force;
    private readonly float _drag;
    private readonly float _accelerationTime;
    
    private KnockbackMovement _knockbackMovement;
    private DazedCameraStrategy _dazedCamera;
    
    private float _fallTimer;
    private readonly float _fallDuration = 1f;
 
    private float _floorTimer;
    private readonly float _timeOnFloor = 2.0f;
 
    private float _getUpTimer;
    private readonly float _timeGettingUp = 1.5f;
    
    private bool _isKnockbackDone;
    private bool _isGettingUp;
    
    private readonly AnimationCurve _getUpCurve;
 
    public PlayerGameplayExplosionState(StateMachine sm, PlayerCharacter player, Vector3 impactDir, 
        float force, float drag, float accelerationTime) : base(sm)
    {
        _player    = player;
        _impactDir = impactDir;
        _force     = force;
        _drag      = drag;
        _accelerationTime = accelerationTime;
        _getUpCurve = CreateGetUpCurve();
    }
 
    public override void OnEnter()
    {
        _player.CharacterController.enabled = false;
        _player.CharacterController.enabled = true;
        SFXManager.SetState("Stunned", "Player_State");

        _knockbackMovement = new KnockbackMovement(_impactDir, _force, _drag, _accelerationTime);
        _player.SetMovementStrategy(_knockbackMovement);
        
        _dazedCamera = new DazedCameraStrategy(_getUpCurve, _impactDir, 0.35f, 2.5f,50f);
 
        _player.CamController.SetCameraStrategy(_dazedCamera);
        _fallTimer      = 0f;
        _floorTimer     = 0f;
        _getUpTimer     = 0f;
        _isKnockbackDone = false;
        _isGettingUp    = false;
    }
 
    public override void Update()
    {
        if (_fallTimer < _fallDuration)
        {
            _fallTimer += Time.deltaTime;
            _dazedCamera.SetFallingProgress(_fallTimer / _fallDuration);
        }
        
        bool isGrounded = _player.CharacterController.isGrounded;
        bool isFalling = _knockbackMovement.CurrentSpeedY <= 0f;
        bool isKnockbackFinished = _knockbackMovement.IsKnockbackFinished() && isFalling && isGrounded;
 
        if (!_isKnockbackDone && isKnockbackFinished && _fallTimer >= _fallDuration)
        {
            _isKnockbackDone = true;
            _dazedCamera.StartOnFloor();
        }
        
        if (_isKnockbackDone && !_isGettingUp)
        {
            _floorTimer += Time.deltaTime;
 
            if (_floorTimer >= _timeOnFloor)
            {
                _isGettingUp = true;
                _dazedCamera.StartGettingUp();  
            }
        }
        
        if (_isGettingUp)
        {
            _getUpTimer += Time.deltaTime;
            _dazedCamera.SetGettingUpProgress(_getUpTimer / _timeGettingUp);
 
            if (_dazedCamera.IsFinished)
            {
                Sm.ChangeState(new PlayerGameplayFreeState(Sm, _player));
            }
        }
    }
 
    public override void OnExit()
    {
        SFXManager.SetState("Alive", "Player_State");
        _player.EnableGameplayInputs();
    }
 
    private AnimationCurve CreateGetUpCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f,0f,0f,0f),
            new Keyframe(0.5f, 0.6f,0.5f, 0f),
            new Keyframe(1f,1.2f,2f,0f)
        );
    }
}