#region Using statements

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#endregion

namespace Bitgem.VFX.StylisedWater
{
    public class WaterVolumeHelper : MonoBehaviour
    {
        #region Private static fields

        private static WaterVolumeHelper instance = null;

        #endregion

        #region Public fields

        public WaterVolumeBase WaterVolume = null;

        #endregion

        #region Public static properties

        public static WaterVolumeHelper Instance { get { return instance; } }

        #endregion

        #region Public methods

        public float? GetHeight(Vector3 _position)
        {
            // ensure a water volume
            if (!WaterVolume)
            {
                return 0f;
            }

            // ensure a material
            var renderer = WaterVolume.gameObject.GetComponent<MeshRenderer>();
            if (!renderer || !renderer.sharedMaterial)
            {
                return 0f;
            }

            // replicate the shader logic, using parameters pulled from the specific material, to return the height at the specified position
            var waterHeight = WaterVolume.GetHeight(_position);
            if (!waterHeight.HasValue)
            {
                return null;
            }
            return waterHeight.Value + GetWaveOffset(_position, renderer.sharedMaterial);
        }

        public bool TryGetSurfaceHeight(Vector3 worldPosition, out float height, bool matchRenderedSurface = false)
        {
            height = 0f;
            if (!WaterVolume || !WaterVolume.isActiveAndEnabled
                || !WaterVolume.TryGetComponent(out MeshRenderer renderer)
                || !renderer.enabled || !renderer.sharedMaterial
                || !WaterVolume.TryGetSurfacePoint(worldPosition, out Vector3 localPoint, out Vector2Int tile, out int tileY))
            {
                return false;
            }

            // The shader samples waves in world XZ and applies their displacement in local Y.
            if (localPoint.y >= 0.5f)
                localPoint.y += matchRenderedSurface
                    ? GetRenderedWaveOffset(localPoint, tile, tileY, renderer.sharedMaterial)
                    : GetWaveOffset(worldPosition, renderer.sharedMaterial);
            height = WaterVolume.transform.TransformPoint(localPoint).y;
            return true;
        }

        private static float GetWaveOffset(Vector3 worldPosition, Material material)
        {
            float frequency = material.GetFloat("_WaveFrequency");
            float scale = material.GetFloat("_WaveScale");
            float time = Time.time * material.GetFloat("_WaveSpeed");
            return EvaluateWave(worldPosition, frequency, scale, time);
        }

        private float GetRenderedWaveOffset(Vector3 localPoint, Vector2Int tile, int tileY, Material material)
        {
            float size = WaterVolume.TileSize;
            Vector3 corner = new(tile.x * size - 0.5f, localPoint.y, tile.y * size - 0.5f);
            float u = Mathf.Clamp01((localPoint.x - corner.x) / size);
            float v = Mathf.Clamp01((localPoint.z - corner.z) / size);
            float frequency = material.GetFloat("_WaveFrequency");
            float scale = material.GetFloat("_WaveScale");
            float time = Time.time * material.GetFloat("_WaveSpeed");
            Transform surface = WaterVolume.transform;

            // Shader Graph displaces the vertices; the rendered height between them is triangular.
            float a = EvaluateWave(surface.TransformPoint(corner), frequency, scale, time);
            float b = EvaluateWave(surface.TransformPoint(corner + Vector3.forward * size), frequency, scale, time);
            float c = EvaluateWave(surface.TransformPoint(corner + new Vector3(size, 0f, size)), frequency, scale, time);
            float d = EvaluateWave(surface.TransformPoint(corner + Vector3.right * size), frequency, scale, time);
            if (WaterVolume.UsesFoamDiagonal(tile, tileY))
                return u + v <= 1f
                    ? a + (d - a) * u + (b - a) * v
                    : c + (b - c) * (1f - u) + (d - c) * (1f - v);

            return u >= v
                ? a + (d - a) * u + (c - d) * v
                : a + (b - a) * v + (c - b) * u;
        }

        private static float EvaluateWave(Vector3 point, float frequency, float scale, float time)
        {
            return (Mathf.Sin(point.x * frequency + time)
                + Mathf.Cos(point.z * frequency + time)) * scale;
        }

        #endregion

        #region MonoBehaviour events

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance()
        {
            instance = null;
        }

        private void Awake()
        {
            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        #endregion
    }
}
