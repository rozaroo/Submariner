using System;

public interface ILeverControls : IControls
{
    public bool isActive { get; set; }
    public Action onDeactivation { get; set; }
    public void OnActionDrag(float delta);
}

