using UnityEngine;

public class PlayerGameplayPossessionState : PlayerGameplayState
{
    private readonly PlayerCharacter _context;
    private readonly IPossessable _station;
    private readonly TutorialSketchbook _bookToOpen;
    private CameraTransition _cameraTransition;
    private string _previousMapName;
    private CursorLockMode _desiredLockMode;
    private bool _showMouse;

    public string PreviousMapName => _previousMapName;
    internal bool IsReadingBook => _bookToOpen != null && _bookToOpen.IsReadingOrWaiting;

    public PlayerGameplayPossessionState(StateMachine sm, PlayerCharacter context, 
        IPossessable station, string previousMapName, CursorLockMode desiredCursorLockMode, bool showMouse,
        TutorialSketchbook bookToOpen) : base(sm)
    {
        _context = context;
        _station = station;
        _previousMapName = previousMapName;
        _desiredLockMode = desiredCursorLockMode;
        _showMouse = showMouse;
        _bookToOpen = bookToOpen;
    }

    public override void OnEnter()
    {
        _context.SetMovementStrategy(new LockedMovement());
        _context?.FootstepSystem.SetActive(false);
        
        CameraPose playerPose = _context.SavedCameraPose;
        CameraPose stationPose = BuildStationPose();
        _cameraTransition = new CameraTransition(playerPose, stationPose, _station.TransitionDuration);
        if (_bookToOpen != null)
        {
            _bookToOpen.WaitForStationCamera();
            _cameraTransition.Completed += OpenBook;
        }
        _context.CamController.SetCameraStrategy(_cameraTransition);
        
        _context.DisableGameplayInputs();
        _context.SetMouseConfiguration(_desiredLockMode, _showMouse);
        _context.Input.SwitchCurrentActionMap(_station.MapName);
        _station.Possess(_context);
    }

    public override void Update() { }

    public override void OnExit()
    {
        _cameraTransition.Completed -= OpenBook;
        _station.UnPossess();
    }

    private void OpenBook()
    {
        _cameraTransition.Completed -= OpenBook;
        if (_bookToOpen != null && _bookToOpen.isActiveAndEnabled)
        {
            _bookToOpen.OpenForReading(_context.CamController.MainCamera);
        }
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
