using System;

public interface IControls
{
    public bool isLocked { get; set; }
    public Action onActivation { get; set; }
    public void Lock();
    public void Unlock();
    public void SetActive(bool active);
    public void Restart();
}

