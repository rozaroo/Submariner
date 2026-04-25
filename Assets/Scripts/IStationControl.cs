using System;

public interface IStationControl
{
    public Action OnActivation { get; set; }
    void OnPointerDown();
    void OnPointerDrag(float deltaY);
    void OnPointerUp();
    void RestartButton();
}
