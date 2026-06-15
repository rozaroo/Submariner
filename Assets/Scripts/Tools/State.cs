public class State : IState
{
    protected StateMachine Sm;

    protected State(StateMachine sm)
    {
        Sm = sm;
    }

    public virtual void OnEnter() { }

    public virtual void Update() { }

    public virtual void OnExit() { }
}