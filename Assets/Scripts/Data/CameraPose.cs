using UnityEngine;

public struct CameraPose
{
    public Vector3 Position;
    public Quaternion Rotation;

    public CameraPose(Vector3 position, Quaternion rotation)
    { 
        Position = position; 
        Rotation = rotation; 
    }
}
