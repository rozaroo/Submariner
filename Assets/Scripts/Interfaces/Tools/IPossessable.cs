using UnityEngine;

public interface IPossessable
{
    string MapName { get; }
    Transform CameraAnchor { get; }
    Transform DirectionAnchor { get; }
    float TransitionDuration { get; }
    CursorLockMode CursorLockMode { get; }
    bool IsMouseVisible { get; }
    
    void Possess(PlayerCharacter player);
    void UnPossess();
}
