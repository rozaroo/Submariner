using UnityEngine;

public class NormalCameraStrategy : ICameraStrategy
{
    public void Enter(CameraContext ctx) { }

    public void Look(CameraContext ctx)
    {
        Vector2 lookDir = ctx.LookAction.ReadValue<Vector2>();
        
        ctx.Yaw += lookDir.x * ctx.LookSensitivity * Time.deltaTime;
        ctx.Pitch -= lookDir.y * ctx.LookSensitivity * Time.deltaTime;
        ctx.Pitch = Mathf.Clamp(ctx.Pitch, -ctx.UpDownPitchLimit, ctx.UpDownPitchLimit);
        
        if (ctx.LookLerpSpeed <= 50)
        {
            float t = 1f - Mathf.Exp(-ctx.LookLerpSpeed * Time.deltaTime);
            ctx.CurrentYaw = Mathf.LerpAngle(ctx.CurrentYaw, ctx.Yaw, t);
            ctx.CurrentPitch = Mathf.LerpAngle(ctx.CurrentPitch, ctx.Pitch, t);
        }
        else
        {
            ctx.CurrentYaw = ctx.Yaw;
            ctx.CurrentPitch = ctx.Pitch;
        }
        
        ctx.PlayerTransform.rotation = Quaternion.Euler(0f, ctx.CurrentYaw, 0f);
        ctx.CameraTransform.localRotation = Quaternion.Euler(ctx.CurrentPitch, 0f, 0f);
    }

    public void Exit(CameraContext ctx) { }
}