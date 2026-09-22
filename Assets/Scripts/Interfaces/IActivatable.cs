using System;
using UnityEngine;

public interface IActivatable
{
    public bool isActive { get; set; }
    public Action onActivation { get; set; }
    public Action onDeactivation { get; set; }
}
