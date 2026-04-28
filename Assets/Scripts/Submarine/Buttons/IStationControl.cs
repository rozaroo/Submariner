using System;

public interface IStationControl
{
    public bool IsUnlocked { get; set; }
    public bool IsPressed { get; set; }
    public Action OnActivation { get; set; }
    void Lock();
    void Unlock();
    void OnActionDown();
    void OnActionDrag(float deltaY);
    void OnActionUp();
    void RestartButton();
}
