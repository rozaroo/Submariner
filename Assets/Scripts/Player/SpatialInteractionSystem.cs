using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public class SpatialInteractionSystem : MonoBehaviour
{
    [Header("Dynamic radii (world units)")]
    [Min(0.1f)] public float discoveryRadius = 7f;
    [Min(0.1f)] public float interactionRadius = 2.5f;
    [Min(0.01f)] public float radiusMultiplier = 1f;
    [Header("Selection and visibility")]
    [Range(1f, 60f)] public float focusAngle = 15f;
    [SerializeField] private LayerMask occlusionLayers = Physics.DefaultRaycastLayers;
    [Header("Appearance")]
    [Tooltip("Sprite shown at discovery distance. Empty uses the generated circle.")]
    [SerializeField] private Sprite discoveryIcon;
    [Tooltip("Optional interaction sprite for all objects. Empty uses each object's InteractionUIData icon.")]
    [SerializeField] private Sprite interactionIconOverride;
    [Min(0.01f)] [SerializeField] private float discoveryIconSize = 0.048f;
    [Min(0.01f)] [SerializeField] private float iconSize = 0.16f;
    [Min(0.1f)] [SerializeField] private float textSize = 2f;
    [SerializeField] private Color normalColor = new(0.75f, 0.9f, 0.85f, 0.85f);
    [SerializeField] private Color focusedColor = Color.white;
    [SerializeField] private TMP_FontAsset font;

    [Header("Icon transition")]
    [Tooltip("Seconds to blend between discovery and interaction. Zero switches instantly.")]
    [Min(0f)] [SerializeField] private float transitionDuration = 0.25f;

    [Header("Interaction outline")]
    [Tooltip("Outline the focused interactable inside its interaction radius.")]
    [SerializeField] private bool enableOutline;
    [Tooltip("Optional mesh outline shader. Creates a runtime material using its defaults.")]
    [SerializeField] private Shader outlineShader;
    [Tooltip("Optional configured outline material. Takes priority over Outline Shader.")]
    [SerializeField] private Material outlineMaterial;

    [Header("Floating motion")]
    [SerializeField] private bool animateIcons = true;
    [Tooltip("Vertical levitation distance in world units. Zero disables levitation.")]
    [Min(0f)] [SerializeField] private float levitationAmplitude = 0.018f;
    [Tooltip("Levitation cycles per second.")]
    [Min(0f)] [SerializeField] private float levitationFrequency = 0.6f;
    [Tooltip("Response to camera movement. Zero disables movement sway.")]
    [Min(0f)] [SerializeField] private float movementSway = 0.025f;
    [Tooltip("Response to camera turning. Zero disables look sway.")]
    [Min(0f)] [SerializeField] private float lookSway = 0.0003f;
    [Tooltip("Maximum sway displacement in world units.")]
    [Min(0f)] [SerializeField] private float maxSwayDistance = 0.05f;
    [Tooltip("How quickly sway responds and returns to the anchor.")]
    [Min(0.1f)] [SerializeField] private float swayResponse = 8f;
    
    [Header("Text opacity")]
    [Tooltip("Multiplicador global de opacidad para todo el texto.")]
    [Range(0f, 1f)] [SerializeField] private float textOpacity = 1f;
    [Tooltip("Opacidad del texto cuando el objeto no está enfocado.")]
    [Range(0f, 1f)] [SerializeField] private float unfocusedTextOpacity = 0.6f;
    [SerializeField] private bool fadeTextWithDistance = true;
    [Tooltip("Opacidad mínima cuando el objeto está en el límite del radio de descubrimiento.")]
    [Range(0f, 1f)] [SerializeField] private float farTextOpacity = 0.25f;

    private readonly Dictionary<SpatialInteractionTarget, SpatialInteractionView> views = new();
    private readonly List<SpatialInteractionTarget> deadTargets = new();
    private readonly HashSet<MonoBehaviour> represented = new();
    private readonly RaycastHit[] hits = new RaycastHit[64];
    private float nextDiscovery;
    private int lastTick = -1;
    private Transform visualRoot;
    private Camera currentCamera;
    private bool hasCameraPose;
    private Vector3 previousCameraPosition;
    private Quaternion previousCameraRotation;
    private Vector3 smoothedSway;
    private SpatialInteractionTarget focusedTarget;
    private readonly SpatialInteractionOutline outline = new();

    public IInteractable ResolveInteraction(PlayerCharacter player, LayerMask interactionLayers)
    {
        if (Time.unscaledTime >= nextDiscovery)
        {
            DiscoverTargets(interactionLayers);
            nextDiscovery = Time.unscaledTime + 1f;
        }

        Camera camera = player.CamController.MainCamera;
        if (currentCamera != camera) hasCameraPose = false;
        currentCamera = camera;

        // Machine surfaces have priority. Visual anchors must never replace their hitboxes.
        focusedTarget = FindSurfaceTarget(player, camera, interactionLayers);
        if (focusedTarget != null) return (IInteractable)focusedTarget.Source;

        // Also allow aiming at an icon, as long as its object is nearby and visible.
        float bestScore = float.PositiveInfinity;
        foreach (var target in SpatialInteractionTarget.Active)
        {
            if (!IsEligible(target, player, interactionLayers) || !target.SpatialUIEnabled) continue;
            float closeRadius = CloseRadius(target);
            if (target.DistanceFrom(camera.transform.position) > closeRadius) continue;
            Vector3 direction = target.Position - camera.transform.position;
            float angle = Vector3.Angle(camera.transform.forward, direction);
            if (angle > focusAngle || angle >= bestScore
                || IsOccluded(camera.transform.position, target.Position, player.transform, target.Source.transform)) continue;
            if (!((IInteractable)target.Source).SendInteractionFeedback(player).canInteract) continue;
            focusedTarget = target;
            bestScore = angle;
        }
        return focusedTarget != null ? (IInteractable)focusedTarget.Source : null;
    }

    private bool IsEligible(SpatialInteractionTarget target, PlayerCharacter player, LayerMask layers) =>
        target != null && target.isActiveAndEnabled && target.Source is IInteractable
        && target.Source.gameObject.activeInHierarchy && !target.Source.transform.IsChildOf(player.transform)
        && (layers.value & (1 << target.Source.gameObject.layer)) != 0;

    private float CloseRadius(SpatialInteractionTarget target) =>
        target.InteractionRadius(interactionRadius, target.DiscoveryRadius(discoveryRadius))
        * Mathf.Max(0.01f, radiusMultiplier);

    private SpatialInteractionTarget FindSurfaceTarget(PlayerCharacter player, Camera camera, LayerMask layers)
    {
        float maxDistance = Mathf.Min(Mathf.Max(0.1f, interactionRadius), Mathf.Max(0.1f, discoveryRadius))
            * Mathf.Max(0.01f, radiusMultiplier);
        foreach (var target in SpatialInteractionTarget.Active)
            if (IsEligible(target, player, layers)) maxDistance = Mathf.Max(maxDistance, CloseRadius(target));

        if (!Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit,
                maxDistance, layers, QueryTriggerInteraction.UseGlobal)) return null;

        // Use the same collider-to-IInteractable lookup as the original player interaction.
        var source = hit.collider.GetComponent<IInteractable>() as MonoBehaviour;
        SpatialInteractionTarget selected = null;
        foreach (var target in SpatialInteractionTarget.Active)
            if (IsEligible(target, player, layers)
                && (target.Source == source || target.gameObject == hit.collider.gameObject))
            { selected = target; break; }

        // A freshly spawned object may not have reached the periodic discovery pass yet.
        if (selected == null && source != null && hit.collider.GetComponent<SpatialInteractionTarget>() == null)
            selected = hit.collider.gameObject.AddComponent<SpatialInteractionTarget>();

        if (!IsEligible(selected, player, layers) || hit.distance > CloseRadius(selected)) return null;
        if (IsOccluded(camera.transform.position, hit.point, player.transform, selected.Source.transform)) return null;
        return ((IInteractable)selected.Source).SendInteractionFeedback(player).canInteract ? selected : null;
    }

    public IInteractable Tick(PlayerCharacter player, LayerMask interactionLayers)
    {
        lastTick = Time.frameCount;
        Camera camera = player.CamController.MainCamera;
        SpatialInteractionTarget selected = focusedTarget;
        foreach (var target in SpatialInteractionTarget.Active)
        {
            if (target == null || target.Source == null || !target.Source.gameObject.activeInHierarchy) continue;
            if (!target.SpatialUIEnabled)
            {
                if (views.TryGetValue(target, out var hiddenView)) hiddenView.Hide();
                continue;
            }
            if (target.Source.transform.IsChildOf(player.transform)) continue;
            if ((interactionLayers.value & (1 << target.Source.gameObject.layer)) == 0) continue;
            float discovery = target.DiscoveryRadius(discoveryRadius) * Mathf.Max(0.01f, radiusMultiplier);
            float closeRadius = target.InteractionRadius(interactionRadius, target.DiscoveryRadius(discoveryRadius))
                * Mathf.Max(0.01f, radiusMultiplier);
            float distance = target.DistanceFrom(camera.transform.position);
            Vector3 direction = target.Position - camera.transform.position;
            bool isFocused = target == selected;
            bool visible = isFocused || (distance <= discovery && Vector3.Dot(camera.transform.forward, direction) > 0f
                && !IsOccluded(camera.transform.position, target.Position, player.transform, target.Source.transform));
            if (!visible) { if (views.TryGetValue(target, out var hidden)) hidden.Hide(); continue; }

            var feedback = ((IInteractable)target.Source).SendInteractionFeedback(player);
            if (string.IsNullOrEmpty(feedback.messageToDisplay))
            { if (views.TryGetValue(target, out var hidden)) hidden.Hide(); continue; }
            bool close = isFocused || distance <= closeRadius;
            if (!views.TryGetValue(target, out var view))
            {
                if (visualRoot == null)
                {
                    visualRoot = new GameObject("Spatial Interaction Visuals").transform;
                    visualRoot.SetParent(transform, false);
                    // World scale must not inherit the player's scaled hierarchy.
                    visualRoot.SetParent(null, true);
                    visualRoot.localScale = Vector3.one;
                }
                view = new SpatialInteractionView(visualRoot, font);
                views.Add(target, view);
            }
            ShowTarget(view, target, camera, feedback, close, isFocused);
        }

        deadTargets.Clear();
        foreach (var entry in views)
        {
            if (entry.Key == null) { entry.Value.Destroy(); deadTargets.Add(entry.Key); }
            else if (entry.Value.LastShownFrame != Time.frameCount || !SpatialInteractionTarget.Active.Contains(entry.Key)
                || entry.Key.Source == null || !entry.Key.Source.gameObject.activeInHierarchy) entry.Value.Hide();
        }
        foreach (var target in deadTargets) views.Remove(target);
        focusedTarget = selected;
        return selected != null ? (IInteractable)selected.Source : null;
    }

    private void ShowTarget(SpatialInteractionView view, SpatialInteractionTarget target, Camera camera,
        InteractionFeedback feedback, bool close, bool focused)
    {
        float multiplier = textOpacity;
        if (!focused)
        {
            multiplier *= unfocusedTextOpacity;
            if (fadeTextWithDistance)
            {
                float scale = Mathf.Max(0.01f, radiusMultiplier);
                float far = target.DiscoveryRadius(discoveryRadius) * scale;
                float near = CloseRadius(target);
                float distance = target.DistanceFrom(camera.transform.position);
                float t = Mathf.InverseLerp(far, near, distance);
                multiplier *= Mathf.Lerp(farTextOpacity, 1f, t);
            }
        }

        Color normal = normalColor;
        normal.a *= multiplier;
        Color focusedCol = focusedColor;
        focusedCol.a *= textOpacity;

        view.Show(target.Position, camera, feedback, close, focused,
            target.DiscoveryIcon(discoveryIcon), target.InteractionIcon(interactionIconOverride),
            target.DiscoveryIconSize(discoveryIconSize),
            Mathf.Max(0.01f, iconSize) * target.IconScale, textSize, normal, focusedCol,
            transitionDuration);
    }

    private void DiscoverTargets(LayerMask layers)
    {
        represented.Clear();
        foreach (var target in SpatialInteractionTarget.Active)
            if (target != null && target.Source != null) represented.Add(target.Source);
        foreach (var component in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (component is not IInteractable || represented.Contains(component)
                || (layers.value & (1 << component.gameObject.layer)) == 0
                || component.GetComponent<Collider>() == null
                || component.GetComponent<SpatialInteractionTarget>() != null) continue;
            component.gameObject.AddComponent<SpatialInteractionTarget>();
        }
    }

    private bool IsOccluded(Vector3 origin, Vector3 destination, Transform player, Transform source)
    {
        Vector3 direction = destination - origin;
        int count = Physics.RaycastNonAlloc(origin, direction.normalized, hits, direction.magnitude,
            occlusionLayers, QueryTriggerInteraction.Ignore);
        // A full buffer is treated as blocked rather than risk interacting through a wall.
        if (count == hits.Length) return true;
        for (int i = 0; i < count; i++)
        {
            Transform hit = hits[i].transform;
            if (hit.IsChildOf(player) || hit.IsChildOf(source)) continue;
            return true;
        }
        return false;
    }

    public void HideAll()
    {
        foreach (var view in views.Values) view.Hide();
        focusedTarget = null;
        outline.Clear();
        hasCameraPose = false;
        smoothedSway = Vector3.zero;
    }

    private void LateUpdate()
    {
        if (lastTick != Time.frameCount || currentCamera == null)
        {
            HideAll();
            return;
        }

        Transform cameraTransform = currentCamera.transform;
        float dt = Time.deltaTime;
        Vector3 desiredSway = Vector3.zero;
        if (animateIcons && hasCameraPose && dt > 0f)
        {
            Vector3 movement = cameraTransform.position - previousCameraPosition;
            float turnAngle = Quaternion.Angle(previousCameraRotation, cameraTransform.rotation);
            // Teleports and camera cuts should not produce a sudden kick in the icons.
            if (movement.sqrMagnitude < 9f && turnAngle < 60f)
            {
                Vector3 velocity = cameraTransform.InverseTransformDirection(movement) / dt;
                Vector3 turn = (Quaternion.Inverse(previousCameraRotation) * cameraTransform.rotation).eulerAngles;
                float pitch = Mathf.DeltaAngle(0f, turn.x) / dt;
                float yaw = Mathf.DeltaAngle(0f, turn.y) / dt;
                desiredSway = new Vector3(-velocity.x * movementSway - yaw * lookSway,
                    -velocity.y * movementSway + pitch * lookSway, -velocity.z * movementSway);
                desiredSway = Vector3.ClampMagnitude(desiredSway, Mathf.Max(0f, maxSwayDistance));
            }
            else smoothedSway = Vector3.zero;
        }
        previousCameraPosition = cameraTransform.position;
        previousCameraRotation = cameraTransform.rotation;
        hasCameraPose = true;
        if (!animateIcons) smoothedSway = Vector3.zero;
        else
        {
            smoothedSway = Vector3.Lerp(smoothedSway, desiredSway,
                1f - Mathf.Exp(-Mathf.Max(0.1f, swayResponse) * dt));
            smoothedSway = Vector3.ClampMagnitude(smoothedSway, Mathf.Max(0f, maxSwayDistance));
        }
        foreach (var entry in views)
            if (entry.Key != null)
                entry.Value.Animate(entry.Key.Position, currentCamera,
                    animateIcons ? Mathf.Max(0f, levitationAmplitude) : 0f,
                    Mathf.Max(0f, levitationFrequency), smoothedSway);
        outline.SetFocus(enableOutline ? focusedTarget : null, outlineMaterial, outlineShader);
        outline.Draw(currentCamera);
    }
    private void OnDisable() => HideAll();
    private void OnDestroy()
    {
        outline.Clear();
        if (visualRoot != null) Destroy(visualRoot.gameObject);
    }
    private void OnValidate()
    {
        discoveryRadius = Mathf.Max(0.1f, discoveryRadius);
        interactionRadius = Mathf.Clamp(interactionRadius, 0.1f, discoveryRadius);
        radiusMultiplier = Mathf.Max(0.01f, radiusMultiplier);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, discoveryRadius * Mathf.Max(0.01f, radiusMultiplier));
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius * Mathf.Max(0.01f, radiusMultiplier));
    }
}
