using System;
using UnityEngine;

public class CameraTransition : ICameraStrategy
{
    private readonly CameraPose _from;
    private readonly CameraPose _to;
    
    private readonly float _duration;
    private float _elapsedTime;
    
    public event Action Completed;
    private bool _completed;
    
    public CameraTransition(CameraPose from, CameraPose to, float duration)
    {
        _from = from;
        _to = to;
        _duration = duration;
    }

    public void Enter(CameraContext ctx)
    {
        _elapsedTime = 0f;
    }

    public void Look(CameraContext ctx)
    {
        _elapsedTime += Time.deltaTime;
        float normalized = Mathf.Clamp01(_elapsedTime / _duration);
        float t = Mathf.SmoothStep(0f, 1f, normalized);

        ctx.CameraTransform.position = Vector3.Lerp(_from.Position, _to.Position, t);
        ctx.CameraTransform.rotation = Quaternion.Slerp(_from.Rotation, _to.Rotation, t);
        
        if(normalized >= 1f && !_completed)
        {
            _completed = true;
            Completed?.Invoke();
        }
    }

    public void Exit(CameraContext ctx)
    {
        ctx.CameraTransform.position = _to.Position;
        ctx.CameraTransform.rotation = _to.Rotation;
    }
}