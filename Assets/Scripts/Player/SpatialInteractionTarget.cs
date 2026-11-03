using System.Collections.Generic;
using UnityEngine;

/// <summary>Optional per-object placement and radius overrides for spatial interaction.</summary>
[DisallowMultipleComponent]
public class SpatialInteractionTarget : MonoBehaviour
{
    internal static readonly HashSet<SpatialInteractionTarget> Active = new();

    [Tooltip("Optional IInteractable component. Defaults to one on this GameObject.")]
    [SerializeField] private MonoBehaviour interactionSource;
    [Tooltip("Optional anchor. Without one, uses the top of the object's collider.")]
    [SerializeField] private Transform anchor;
    [SerializeField] private Vector3 localOffset = new(0f, 0.2f, 0f);
    [SerializeField] private bool overrideRadii;
    [Min(0.1f)] [SerializeField] private float discoveryRadius = 7f;
    [Min(0.1f)] [SerializeField] private float interactionRadius = 2.5f;
    [Tooltip("Scales both radii at runtime. Useful for upgrades or changing conditions.")]
    [Min(0.01f)] public float radiusMultiplier = 1f;

    [Header("Icon overrides (optional)")]
    [Tooltip("Discovery sprite for this object. Empty inherits the player's icon.")]
    [SerializeField] private Sprite discoveryIcon;
    [Tooltip("Interaction sprite for this object. Empty inherits the player's override or InteractionUIData icon.")]
    [SerializeField] private Sprite interactionIconOverride;
    [Tooltip("Multiplies both icon sizes for this object.")]
    [Min(0.01f)] [SerializeField] private float iconScale = 1f;
    [Tooltip("Use a separate discovery icon size for this object.")]
    [SerializeField] private bool overrideDiscoveryIconSize;
    [Min(0.01f)] [SerializeField] private float discoveryIconSize = 0.048f;

    [Header("Interaction outline")]
    [Tooltip("Allow the interaction system to outline this object when focused.")]
    [SerializeField] private bool allowOutline = true;
    [Tooltip("Meshes to outline (for example the whole table). Empty uses renderers under Interaction Source.")]
    [SerializeField] private Renderer[] outlineRenderers;

    public bool AllowOutline => allowOutline;
    public Renderer[] GetOutlineRenderers() => outlineRenderers != null && outlineRenderers.Length > 0
        ? outlineRenderers : Source != null ? Source.GetComponentsInChildren<Renderer>() : System.Array.Empty<Renderer>();

    public Sprite DiscoveryIcon(Sprite fallback) => discoveryIcon != null ? discoveryIcon : fallback;
    public Sprite InteractionIcon(Sprite fallback) => interactionIconOverride != null ? interactionIconOverride : fallback;
    public float IconScale => Mathf.Max(0.01f, iconScale);
    public float DiscoveryIconSize(float fallback) =>
        Mathf.Max(0.01f, overrideDiscoveryIconSize ? discoveryIconSize : fallback) * IconScale;

    public MonoBehaviour Source
    {
        get
        {
            if (interactionSource != null) return interactionSource;
            foreach (var component in GetComponents<MonoBehaviour>())
                if (component is IInteractable)
                {
                    interactionSource = component;
                    return component;
                }
            return null;
        }
    }

    public Vector3 Position
    {
        get
        {
            if (anchor != null) return anchor.TransformPoint(localOffset);
            var boundsCollider = GetComponent<Collider>();
            Vector3 top = boundsCollider != null
                ? new Vector3(boundsCollider.bounds.center.x, boundsCollider.bounds.max.y, boundsCollider.bounds.center.z)
                : transform.position;
            return top + transform.TransformVector(localOffset);
        }
    }

    // Distance is measured to the object, independently of the visual anchor.
    public Vector3 DistanceOrigin => Source != null ? Source.transform.position : transform.position;
    public float DiscoveryRadius(float fallback) => Mathf.Max(0.1f,
        (overrideRadii ? discoveryRadius : fallback) * Mathf.Max(0.01f, radiusMultiplier));
    public float InteractionRadius(float fallback, float discovery) => Mathf.Min(discovery,
        Mathf.Max(0.1f, (overrideRadii ? interactionRadius : fallback) * Mathf.Max(0.01f, radiusMultiplier)));

    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);

    private void OnValidate()
    {
        discoveryRadius = Mathf.Max(0.1f, discoveryRadius);
        interactionRadius = Mathf.Clamp(interactionRadius, 0.1f, discoveryRadius);
        radiusMultiplier = Mathf.Max(0.01f, radiusMultiplier);
        iconScale = Mathf.Max(0.01f, iconScale);
        discoveryIconSize = Mathf.Max(0.01f, discoveryIconSize);
        if (interactionSource != null && interactionSource is not IInteractable)
            interactionSource = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Position, 0.08f);
        if (!overrideRadii) return;
        Gizmos.DrawWireSphere(DistanceOrigin, DiscoveryRadius(discoveryRadius));
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(DistanceOrigin, InteractionRadius(interactionRadius, DiscoveryRadius(discoveryRadius)));
    }
}
