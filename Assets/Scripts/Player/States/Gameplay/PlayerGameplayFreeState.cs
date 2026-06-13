using UnityEngine;

public class PlayerGameplayFreeState : PlayerGameplayState
{
    private readonly PlayerCharacter _context;

    public PlayerGameplayFreeState(StateMachine sm, PlayerCharacter context) : base(sm)
    {
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

    public override void Update() { }

    public override void OnExit()
    {
        _context.DisableGameplayInputs();
    }
}