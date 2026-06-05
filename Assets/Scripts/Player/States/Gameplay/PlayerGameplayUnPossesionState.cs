using UnityEngine;

public class PlayerGameplayUnPossessionState : PlayerGameplayState
{
    private readonly PlayerCharacter _player;
    private readonly IPossessable _station;

    private CameraTransition _transition;

    public PlayerGameplayUnPossessionState(StateMachine sm, PlayerCharacter player, IPossessable station) : base(sm)
    {
        _player = player;
        _station = station;
    }

    public override void OnEnter()
    {
        CameraPose stationPose = BuildStationPose();
        CameraPose playerPose = _player.SavedCameraPose;
        _transition = new CameraTransition(stationPose, playerPose, _station.TransitionDuration);
        _transition.Completed += OnTransitionFinished;
        _player.SetMovementStrategy(new LockedMovement());
        _player.camController.SetCameraStrategy(_transition);
    }

    public override void Update() { }

    public override void OnExit()
    {
        if (_transition != null)
            _transition.Completed -= OnTransitionFinished;
    }
    
    private void OnTransitionFinished()
    {
        Sm.ChangeState(new PlayerGameplayFreeState(Sm, _player));
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