using UnityEngine;

public interface ICameraRotation
{
    float Yaw { get; set; }
    float Pitch { get; set; }
    float CurrentYaw { get; set; }
    float CurrentPitch { get; set; }
}