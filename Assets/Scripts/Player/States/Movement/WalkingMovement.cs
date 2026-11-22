using Bitgem.VFX.StylisedWater;
using UnityEngine;

public class WalkingMovement : IMovementStrategy
{
    private const float FullResistanceHeightFraction = 0.65f;
    private const float SpeedAdjustmentRate = 2.5f;
    private readonly WaterVolumeHelper _floodWater;
    private readonly Renderer _floodSurface;
    private readonly float _submergedSpeedMultiplier;

    private float _waterSpeedMultiplier = 1f;
    private float _moveVelocityY;
    private float _gravity = 9.81f;

    public WalkingMovement(WaterVolumeHelper floodWater = null, float submergedSpeedMultiplier = 0.45f)
    {
        _floodWater = floodWater;
        _submergedSpeedMultiplier = submergedSpeedMultiplier;
        if (floodWater && floodWater.WaterVolume)
            _floodSurface = floodWater.WaterVolume.GetComponent<Renderer>();
    }

    public void Move(MovementContext ctx)
    {
        Vector2 input = ctx.MovementAction.ReadValue<Vector2>();
        _waterSpeedMultiplier = Mathf.MoveTowards(_waterSpeedMultiplier, GetWaterSpeedMultiplier(ctx.CharacterController),
            SpeedAdjustmentRate * Time.deltaTime);
        
        Vector3 move = (ctx.Transform.right * input.x + ctx.Transform.forward * input.y) * _waterSpeedMultiplier;
        if (ctx.CharacterController.isGrounded && _moveVelocityY < 0) _moveVelocityY = -2f;
        _moveVelocityY += -_gravity * Time.deltaTime;
        move.y = _moveVelocityY;
        ctx.CharacterController.Move(move * ctx.MoveSpeed * Time.deltaTime);
    }

    private float GetWaterSpeedMultiplier(CharacterController controller)
    {
        if (!_floodWater || !_floodWater.isActiveAndEnabled || !_floodSurface)
            return 1f;

        Bounds body = controller.bounds;
        if (!_floodWater.TryGetSurfaceHeight(body.center, out float waterHeight, matchRenderedSurface: true))
            return 1f;

        float submergedHeight = Mathf.Min(body.max.y, waterHeight) - Mathf.Max(body.min.y, _floodSurface.bounds.min.y);
        float resistance = Mathf.InverseLerp(0f, body.size.y * FullResistanceHeightFraction, submergedHeight);
        return Mathf.Lerp(1f, _submergedSpeedMultiplier, resistance);
    }
}
