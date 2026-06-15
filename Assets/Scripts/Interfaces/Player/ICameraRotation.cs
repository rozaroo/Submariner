public interface ICameraRotation
{
    float Yaw { get; set; }
    float Pitch { get; set; }
    float CurrentYaw { get; set; } //WARNING: Takes Player Rotation Yaw, NOT Camera Local Rotation Yaw
    float CurrentPitch { get; set; }
}