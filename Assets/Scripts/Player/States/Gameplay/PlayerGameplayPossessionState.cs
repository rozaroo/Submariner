using UnityEngine;

public class PlayerGameplayPossessionState : PlayerGameplayState
{
    private readonly PlayerCharacter _context;
    private readonly IPossessable _station;
    private string _previousMapName;


    public PlayerGameplayPossessionState(StateMachine sm, PlayerCharacter context, 
        IPossessable station, string previousMapName) : base(sm)
    {
        _context = context;
        _station = station;
        _previousMapName = previousMapName;
    }

    public override void OnEnter()
    {
        _context.SetMovementStrategy(new LockedMovement());
        
        CameraPose playerPose = _context.SavedCameraPose;
        CameraPose stationPose = BuildStationPose();
        _context.camController.SetCameraStrategy(
            new CameraTransition(playerPose, stationPose, _station.TransitionDuration));
        
        _context.DisableGameplayInputs();
        _context.Input.SwitchCurrentActionMap(_station.MapName);
        _station.Possess(_context);
    }

    public override void Update() { }

    public override void OnExit()
    {
        _station.UnPossess();
        _context.Input.SwitchCurrentActionMap(_previousMapName);
    }
    
    private CameraPose BuildStationPose()
    {
        Transform cameraAnchor = _station.CameraAnchor;
        Transform directionAnchor = _station.DirectionAnchor;

        Vector3 direction = directionAnchor.position - cameraAnchor.position;
        Quaternion rotation = Quaternion.LookRotation(direction);

        return new CameraPose(cameraAnchor.position, rotation);
    }
}
