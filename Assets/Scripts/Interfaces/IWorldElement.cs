using System;
using UnityEngine;

public interface IWorldElement
{
    Vector3 Position { get; }
    Vector3 Rotation { get; }
    SonarDetectionMode SonarDetectionMode { get; }
    event Action<IWorldElement> OnElementDestroyed; 
}
