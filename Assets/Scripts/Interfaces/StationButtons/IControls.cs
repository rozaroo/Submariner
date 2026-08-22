using UnityEngine.Events;
public interface IControls
{
    public bool isLocked { get; set; }
    UnityEvent onActivation { get; }
    public void Lock();
    public void Unlock();
    public void SetActive(bool active);
    public void Restart();
}

