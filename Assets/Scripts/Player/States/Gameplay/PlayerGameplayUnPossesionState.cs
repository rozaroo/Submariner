using UnityEngine;

public class PlayerGameplayUnPossessionState : PlayerGameplayState
{
    private readonly PlayerCharacter _player;
    private readonly IPossessable _station;
    private readonly string _mapToReturn;

    public PlayerGameplayUnPossessionState(StateMachine sm, PlayerCharacter player, IPossessable station, string mapToReturn) : base(sm)
    {
        _player = player;
        _station = station;
        _mapToReturn = mapToReturn;
    }

    public override void OnEnter()
    {
        Log.Info("[UNPOSSESSION] OnEnter");

        Log.Info($"[UNPOSSESSION] Player: {_player}");
        Log.Info($"[UNPOSSESSION] Station: {_station}");
        Log.Info($"[UNPOSSESSION] Map To Return: '{_mapToReturn}'");
        Log.Info($"[UNPOSSESSION] Input: {_player?.Input}");

        _player.SetMovementStrategy(new LockedMovement());

        _player.SetMouseConfiguration(CursorLockMode.Locked, false);

        if (string.IsNullOrEmpty(_mapToReturn))
        {
            Log.Error("[UNPOSSESSION] Map To Return is NULL or EMPTY!");
            return;
        }

        _player.Input.SwitchCurrentActionMap(_mapToReturn);

        Log.Info("[UNPOSSESSION] Action Map switched successfully.");

        Sm.ChangeState(new PlayerGameplayFreeState(Sm, _player));
    }

    public override void Update() { }
    
    private void OnTransitionFinished()
    {
        _player.SetMouseConfiguration(CursorLockMode.Locked, false);
        _player.Input.SwitchCurrentActionMap(_mapToReturn);
        Sm.ChangeState(new PlayerGameplayFreeState(Sm, _player));
    }
}