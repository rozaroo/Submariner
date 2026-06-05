using UnityEngine;

public interface ICameraStrategy
{
    void Enter(CameraContext ctx);
    void Look(CameraContext ctx);
    void Exit(CameraContext ctx);
}