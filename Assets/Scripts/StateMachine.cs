public class StateMachine
{
    private IState _currentState;
    
    public void SetInitialState(IState state)
    {
        _currentState = state;
        _currentState.OnEnter();
    }

    public void ChangeState(IState newState)
    {
        _currentState?.OnExit();

        _currentState = newState;

        _currentState?.OnEnter();
    }

    public void Update()
    {
        _currentState?.Update();
    }
}