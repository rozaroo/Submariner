using UnityEngine;

public class PlayerGameplayPossessionState : IState
{
    private PlayerCharacter _context;
    private IPossessable _station;
    private bool _needsTransition;
    private string _previousMapName;

    public PlayerGameplayPossessionState(PlayerCharacter context, IPossessable station, bool needsTransition)
    {
        _context = context;
        _station = station;
        _needsTransition = needsTransition;
    }
    
    public void OnEnter()
    {
        _context.playerMovementSm.ChangeState(_context.lockedMovementState);
        _previousMapName = _context.input.currentActionMap.name;
        
        _context.input.SwitchCurrentActionMap(_station.MapName);
        
        if (_needsTransition)
        {
            _context.camController.ForceMoveLookCamera(
                _station.CameraAnchor.position,
                _station.DirectionAnchor.position, 
                _station.TransitionDuration);
        }
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        _station.Possess(_context);
    }

    public void Update() { }

    public void LateUpdate() { }

    public void OnExit()
    {
        _station.UnPossess();

        if (_needsTransition)
        {
            _context.camController.ReturnToStartingPosition(_station.TransitionDuration);   
        }
        
        _context.input.SwitchCurrentActionMap(_previousMapName);
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
