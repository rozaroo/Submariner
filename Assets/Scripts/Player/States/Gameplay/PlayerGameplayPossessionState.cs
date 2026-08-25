using UnityEngine;

public class PlayerGameplayPossessionState : PlayerGameplayState
{
    private readonly PlayerCharacter _context;
    private readonly IPossessable _station;
    private string _previousMapName;

    public string PreviousMapName => _previousMapName;

    public PlayerGameplayPossessionState(StateMachine sm, PlayerCharacter context, 
        IPossessable station, string previousMapName) : base(sm)
    {
        _context = context;
        _station = station;
    }

    public override void OnEnter()
    {
        _context.SetMovementStrategy(new LockedMovement());
        _context?.FootstepSystem.SetActive(false);
        
        CameraPose playerPose = _context.SavedCameraPose;
        
        _context.DisableGameplayInputs();
        _context.Input.SwitchCurrentActionMap(_station.MapName);
    }

    public override void Update() { }

    public override void OnExit() { }
    
}
