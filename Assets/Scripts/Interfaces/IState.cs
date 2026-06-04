using System;

public interface IState
{
    void OnEnter();
    void Update();
    void LateUpdate();
    void OnExit();
}
