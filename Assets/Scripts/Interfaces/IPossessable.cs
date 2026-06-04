using UnityEngine;

public interface IPossessable
{
    string MapName { get; }
    Transform CameraAnchor { get; }
    Transform DirectionAnchor { get; }
    float TransitionDuration { get; }
    
    void Possess(PlayerCharacter player);
    void UnPossess();
}
