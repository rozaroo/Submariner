public interface IMainWorldEvent
{
    bool CheckConditions();
    void Execute();
    void EndEvent();
}