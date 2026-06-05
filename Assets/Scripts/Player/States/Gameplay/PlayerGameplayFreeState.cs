public class PlayerGameplayFreeState : PlayerGameplayState
{
    private readonly PlayerCharacter _player;

    public PlayerGameplayFreeState(StateMachine sm, PlayerCharacter player) : base(sm)
    {
        _player = player;
    }

    public override void OnEnter()
    {
        _player.SetMovementStrategy(new WalkingMovement());
        _player.camController.SetCameraStrategy(new NormalCameraStrategy());
        
        _player.EnableGameplayInputs();
    }

    public override void Update() { }

    public override void LateUpdate() { }

    public override void OnExit()
    {
        _player.DisableGameplayInputs();
    }
}