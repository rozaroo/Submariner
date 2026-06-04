using UnityEngine;

public interface ITransferable
{
    public IState nextState {get;}
    public bool isComplete {get;}
}
