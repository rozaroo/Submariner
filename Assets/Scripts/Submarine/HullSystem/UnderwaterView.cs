using Bitgem.VFX.StylisedWater;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class UnderwaterView : MonoBehaviour
{
    [SerializeField] private WaterVolumeHelper water;
    [SerializeField, Min(0.005f)] private float fullEffectDepth = 0.05f;
    [SerializeField, Range(0f, 5f)] private float blurRadius = 2.4f;
    [SerializeField] private Color oceanColor = new(0.12f, 0.38f, 0.45f);
    [SerializeField, Min(0f)] private float hazeDensity = 0.32f;

    private Renderer surfaceRenderer;

    internal Color OceanColor => oceanColor;
    internal float HazeDensity => hazeDensity;

    private void Awake()
    {
        if (water && water.WaterVolume)
            surfaceRenderer = water.WaterVolume.GetComponent<Renderer>();
    }

    internal bool TryGetEffect(out Vector3 parameters)
    {
        parameters = Vector3.zero;
        if (!isActiveAndEnabled || (blurRadius <= 0f && hazeDensity <= 0f)
            || !water || !water.isActiveAndEnabled || !surfaceRenderer)
            return false;

        Vector3 eye = transform.position;
        if (eye.y < surfaceRenderer.bounds.min.y
            || !water.TryGetSurfaceHeight(eye, out float height, matchRenderedSurface: true))
            return false;

        float depth = height - eye.y;
        float weight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, fullEffectDepth, depth));
        parameters = new Vector3(blurRadius * weight, weight, depth);
        return weight > 0f;
    }
}
