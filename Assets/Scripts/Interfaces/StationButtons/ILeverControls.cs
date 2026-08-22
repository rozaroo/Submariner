using UnityEngine.Events;
public interface ILeverControls : IControls
{
    public bool isActive { get; set; }
    UnityEvent onDeactivation { get; }
    public void OnActionDrag(float delta);
}

