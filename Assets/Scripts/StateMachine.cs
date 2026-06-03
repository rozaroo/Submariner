public class StateMachine
{
    private IState currentState { get; set; }
    
    public void ChangeState(IState state)
    {
        if (currentState != null)
        {
            currentState.OnExit();
        }

        if (state == null) return;
        currentState = state;
        currentState.OnEnter();
    }
    
    public void Update() => currentState?.Update();
    
    public void LateUpdate() => currentState?.LateUpdate();
}
