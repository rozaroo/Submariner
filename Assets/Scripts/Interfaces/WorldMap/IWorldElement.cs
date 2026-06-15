using System;
using UnityEngine;

public interface IWorldElement
{
    Vector3 position { get; }
    Vector3 rotation { get; }
    SonarDetectionMode sonarDetectionMode { get; }
    event Action<IWorldElement> OnElementDestroyed; 
}
