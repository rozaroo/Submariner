#region Using statements

using UnityEngine;

#endregion

namespace Bitgem.VFX.StylisedWater
{
    [DisallowMultipleComponent]
    public class WateverVolumeFloater : MonoBehaviour
    {
        private const float VerticalResistance = 5f;
        private const float HorizontalResistance = 3f;
        private static readonly Vector3[] SampleOffsets =
        {
            new(-1f, -1f, -1f), new(-1f, -1f, 1f),
            new(1f, -1f, -1f), new(1f, -1f, 1f),
            new(-1f, 1f, -1f), new(-1f, 1f, 1f),
            new(1f, 1f, -1f), new(1f, 1f, 1f)
        };

        #region Public fields

        public WaterVolumeHelper WaterVolumeHelper = null;
        [Tooltip("Float using Rigidbody forces sampled across the Box Collider.")]
        [SerializeField] private bool useBuoyancy;
        [Tooltip("Fraction of the collider that rests below the surface in calm water.")]
        [Range(0.1f, 0.9f)] [SerializeField] private float restingSubmersion = 0.55f;

        private Rigidbody body;
        private BoxCollider bodyCollider;

        #endregion

        #region MonoBehaviour events

        private void Awake()
        {
            if (!useBuoyancy) return;
            body = GetComponent<Rigidbody>();
            bodyCollider = GetComponent<BoxCollider>();
        }

        private void Update()
        {
            if (useBuoyancy) return;
            var instance = WaterVolumeHelper ? WaterVolumeHelper : WaterVolumeHelper.Instance;
            if (!instance)
            {
                return;
            }

            transform.position = new Vector3(transform.position.x, instance.GetHeight(transform.position) ?? transform.position.y, transform.position.z);
        }

        private void FixedUpdate()
        {
            if (!useBuoyancy || !body || body.isKinematic || !bodyCollider || !bodyCollider.enabled)
                return;

            var helper = WaterVolumeHelper ? WaterVolumeHelper : WaterVolumeHelper.Instance;
            if (!helper || !helper.isActiveAndEnabled) return;

            // Sample the centers of eight equal box sections to apply lift where the item is wet.
            Vector3 quarterSize = bodyCollider.size * 0.25f;
            float sampleHeight = Mathf.Max(0.01f, bodyCollider.bounds.extents.y);
            float massPerSample = body.mass / SampleOffsets.Length;
            float buoyancy = body.useGravity ? -Physics.gravity.y / restingSubmersion : 0f;

            foreach (Vector3 offset in SampleOffsets)
            {
                Vector3 localPoint = bodyCollider.center + Vector3.Scale(quarterSize, offset);
                Vector3 point = transform.TransformPoint(localPoint);
                if (!helper.TryGetSurfaceHeight(point, out float surfaceHeight)) continue;

                float submergedFraction = Mathf.Clamp01(0.5f + (surfaceHeight - point.y) / sampleHeight);
                if (submergedFraction <= 0f) continue;

                Vector3 velocity = body.GetPointVelocity(point);
                Vector3 force = new(
                    -velocity.x * HorizontalResistance,
                    Mathf.Max(0f, buoyancy - velocity.y * VerticalResistance),
                    -velocity.z * HorizontalResistance);
                body.AddForceAtPosition(force * (massPerSample * submergedFraction), point);
            }
        }

        #endregion
    }
}
