using UnityEngine;

public class NormalCameraStrategy : ICameraStrategy
{
    public void Enter(CameraContext ctx)
    {
        if(ctx == null) return;
        float currentBodyYaw = ctx.PlayerTransform.eulerAngles.y;
        
        ctx.InputYaw = currentBodyYaw;
        ctx.SmoothedYaw = currentBodyYaw;
        
        float currentCamPitch = ctx.CameraTransform.localEulerAngles.x;
        
        if (currentCamPitch > 180f) 
        {
            currentCamPitch -= 360f;
        }

        ctx.InputPitch = currentCamPitch;
        ctx.SmoothedPitch = currentCamPitch;
    }

    public void Look(CameraContext ctx)
    {
        Vector2 lookDir = ctx.LookAction.ReadValue<Vector2>();
        
        ctx.InputYaw += lookDir.x * ctx.LookSensitivity * Time.deltaTime;
        ctx.InputPitch -= lookDir.y * ctx.LookSensitivity * Time.deltaTime;
        ctx.InputPitch = Mathf.Clamp(ctx.InputPitch, -ctx.UpDownPitchLimit, ctx.UpDownPitchLimit);
        
        if (ctx.LookLerpSpeed <= 50)
        {
            float t = 1f - Mathf.Exp(-ctx.LookLerpSpeed * Time.deltaTime);
            ctx.SmoothedYaw = Mathf.LerpAngle(ctx.SmoothedYaw, ctx.InputYaw, t);
            ctx.SmoothedPitch = Mathf.LerpAngle(ctx.SmoothedPitch, ctx.InputPitch, t);
        }
        else
        {
            ctx.SmoothedYaw = ctx.InputYaw;
            ctx.SmoothedPitch = ctx.InputPitch;
        }
        
        ctx.PlayerTransform.rotation = Quaternion.Euler(0f, ctx.SmoothedYaw, 0f);
        ctx.CameraTransform.localRotation = Quaternion.Euler(ctx.SmoothedPitch, 0f, 0f);
    }

    public void Exit(CameraContext ctx) { }
}