using System;

public interface IControls: IActivatable
{
    public bool isLocked { get; set; }
    public void Lock();
    public void Unlock();
    public void SetActive(bool active);
    public void Restart();
}

