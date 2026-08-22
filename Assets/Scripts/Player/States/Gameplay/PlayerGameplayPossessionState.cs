using UnityEngine;

public class PlayerGameplayPossessionState : PlayerGameplayState
{
    private readonly PlayerCharacter _context;
    private readonly IPossessable _station;
    private string _previousMapName;
    private CursorLockMode _desiredLockMode;
    private bool _showMouse;

    public string PreviousMapName => _previousMapName;

    public PlayerGameplayPossessionState(StateMachine sm, PlayerCharacter context, 
        IPossessable station, string previousMapName, CursorLockMode desiredCursorLockMode, bool showMouse) : base(sm)
    {
        _context = context;
        _station = station;
        _previousMapName = previousMapName;
        _desiredLockMode = desiredCursorLockMode;
        _showMouse = showMouse;
    }

    public override void OnEnter()
    {
        _context.SetMovementStrategy(new LockedMovement());
        _context?.FootstepSystem.SetActive(false);
        
        CameraPose playerPose = _context.SavedCameraPose;
        CameraPose stationPose = BuildStationPose();
        _context.CamController.SetCameraStrategy(new CameraTransition(playerPose, stationPose, _station.TransitionDuration));
        
        _context.DisableGameplayInputs();
        _context.SetMouseConfiguration(_desiredLockMode, _showMouse);
        _context.Input.SwitchCurrentActionMap(_station.MapName);
    }

    public override void Update() { }

    public override void OnExit() { }
    
    private CameraPose BuildStationPose()
    {
        Transform cameraAnchor = _station.CameraAnchor;
        Transform directionAnchor = _station.DirectionAnchor;

        Vector3 direction = directionAnchor.position - cameraAnchor.position;
        Quaternion rotation = Quaternion.LookRotation(direction);

        return new CameraPose(cameraAnchor.position, rotation);
    }
}
