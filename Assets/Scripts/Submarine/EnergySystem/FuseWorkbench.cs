using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class FuseWorkbench : MonoBehaviour, IInteractable, IPossessable
{
    private const float DragHeightSpeed = 5f;
    private const float DragRotationSpeed = 540f;

    [Header("Possession Config")]
    [SerializeField] private Transform cameraAnchor;
    [SerializeField] private Transform directionAnchor;
    [SerializeField] private float transitionDuration = 0.1f;
    [SerializeField] private CursorLockMode cursorLockMode;
    [SerializeField] private bool showMouseCursor;

    [Header("Actions Maps Settings")]
    [SerializeField] private string playerMapName;
    [SerializeField] private string stationMapName;

    [Header("Input Settings")]
    [SerializeField] private string clickActionName;
    [SerializeField] private string exitActionName;
    [SerializeField] private float raycastDistance = 5f;

    [Header("Fuse Output")]
    [SerializeField] private FuseRecipeCatalogSO fuseRecipeCatalog;
    [SerializeField] private Fuse assembledFusePrefab;
    [SerializeField] private Transform assembledFuseSpawnPoint;
    [SerializeField] private bool destroyPreviousFuseOnAssembly = true;

    [Header("Assembly Plane")]
    [SerializeField] private Transform workPlaneAnchor;
    [SerializeField] private float dragSurfaceOffset = 0.02f;
    [SerializeField] private float partSnapRadius = 0.25f;

    [Header("Assembly Snap Points")]
    [SerializeField] private Transform topAssemblyPoint;
    [SerializeField] private Transform coreAssemblyPoint;
    [SerializeField] private Transform bottomAssemblyPoint;

    [Header("Solder Targets Relative to Core Assembly Point")]
    [SerializeField] private Vector3 topSolderOffset = new Vector3(0.045f, -0.108f, 0f);
    [SerializeField] private Vector3 bottomSolderOffset = new Vector3(0.045f, 0.13f, 0f);
    
    [Header("Interaction")]
    [SerializeField] private InteractionUIData interactionUIData;
    
    [Header("Audio (Wwise)")]
    [SerializeField] private string blowtorchStartEvent = "Start_FuseMake";
    [SerializeField] private string blowtorchStopEvent = "Stop_FuseMake";
    [SerializeField] private float solderDuration = 1f;

    [Header("Top Parts")]
    [SerializeField] private FusePart topPartPrefab;
    [SerializeField] private List<Transform> topPartSpawnPoints = new();
    [SerializeField] private List<TMP_Text> topPartLabels = new();

    [Header("Core Parts")]
    [SerializeField] private FusePart corePartPrefab;
    [SerializeField] private List<Transform> corePartSpawnPoints = new();
    [SerializeField] private List<TMP_Text> corePartLabels = new();

    [Header("Bottom Parts")]
    [SerializeField] private FusePart bottomPartPrefab;
    [SerializeField] private List<Transform> bottomPartSpawnPoints = new();
    [SerializeField] private List<TMP_Text> bottomPartLabels = new();

    private readonly List<FusePart> _spawnedParts = new();

    private PlayerCharacter _currentPlayer;
    private Camera _playerCamera;
    private FusePart _snappedTopPart;
    private FusePart _snappedCorePart;
    private FusePart _snappedBottomPart;
    private FusePart _draggedPart;
    private Vector2 _dragViewportOffset;
    private Fuse _assembledFuse;
    private Coroutine _dragCoroutine;
    private Coroutine _handheldSolderCoroutine;
    private Blowtorch _solderingTorch;
    private bool _isTopConnectionSoldered;
    private bool _isBottomConnectionSoldered;

    public string MapName => stationMapName;
    public Transform CameraAnchor => cameraAnchor;
    public Transform DirectionAnchor => directionAnchor;
    public float TransitionDuration => transitionDuration;
    public CursorLockMode CursorLockMode => cursorLockMode;
    public bool IsMouseVisible => showMouseCursor;
    private Vector3 WorkPlaneNormal => workPlaneAnchor != null ? workPlaneAnchor.up : Vector3.up;

    private void Awake()
    {
        GenerateParts();
    }

    public void Interact(PlayerCharacter player)
    {
        // With the blowtorch equipped, interacting with the bench welds the next
        // pending joint instead of entering the part-placement view.
        if (player.InventorySystem.HeldItem is Blowtorch blowtorch)
        {
            TrySolderWithHandheldBlowtorch(blowtorch);
            return;
        }

        player.OnPossessionState(this);
    }

    public InteractionFeedback SendInteractionFeedback(PlayerCharacter player)
    {
        if (player != null && player.InventorySystem != null)
        {
            if (player.InventorySystem.HeldItem is Blowtorch blowtorch)
            {
                return new InteractionFeedback("Create Fuse", interactionUIData);
            }
        }
        return new InteractionFeedback("Enter Fuse Workbench", interactionUIData);
    }

    /// <summary>
    /// Starts one solder operation using the player's equipped blowtorch.
    /// The bench keeps ownership of the FuseMake audio because it is the
    /// object that knows when a valid connection has completed.
    /// </summary>
    public bool TrySolderWithHandheldBlowtorch(Blowtorch blowtorch)
    {
        if (_handheldSolderCoroutine != null)
        {
            return _solderingTorch == blowtorch;
        }

        FuseWorkbenchConnectionType target = GetNextPendingSolderTarget();
        if (target == FuseWorkbenchConnectionType.None || coreAssemblyPoint == null)
        {
            return false;
        }

        enabled = true;
        Vector3 jointOffset = target == FuseWorkbenchConnectionType.TopToCore ? topSolderOffset : bottomSolderOffset;
        _solderingTorch = blowtorch;
        blowtorch.BeginFuseSoldering(this, coreAssemblyPoint.TransformPoint(jointOffset));
        _handheldSolderCoroutine = StartCoroutine(SolderWithHandheldBlowtorch(target));
        return true;
    }

    public void CancelHandheldSoldering()
    {
        if (_handheldSolderCoroutine == null)
        {
            return;
        }

        StopCoroutine(_handheldSolderCoroutine);
        EndHandheldSoldering();
    }

    private void OnDisable()
    {
        CancelHandheldSoldering();
    }
    
    public void Possess(PlayerCharacter playerCharacter)
    {
        _currentPlayer = playerCharacter;
        _playerCamera = playerCharacter.CamController.MainCamera;
        
        InputAction clickAction = _currentPlayer.Input.actions[clickActionName];
        InputAction exitAction = _currentPlayer.Input.actions[exitActionName];
        
        clickAction.started += OnClickStarted;
        clickAction.canceled += OnClickCanceled;
        exitAction.started += OnExitPerformed;

        enabled = true;
    }

    public void UnPossess()
    {
        InputAction clickAction = _currentPlayer.Input.actions[clickActionName];
        InputAction exitAction = _currentPlayer.Input.actions[exitActionName];

        clickAction.started -= OnClickStarted;
        clickAction.canceled -= OnClickCanceled;
        exitAction.started -= OnExitPerformed;
        
        StopDrag();
        _currentPlayer = null;
        _playerCamera = null;
        enabled = false;
    }

    private void OnClickStarted(InputAction.CallbackContext context)
    {
        if (StationClickBlocker.TryConsumeClick(_playerCamera))
        {
            return;
        }

        if (_playerCamera == null || !TryGetPointerViewport(out Vector2 pointerViewport))
        {
            return;
        }

        FusePart part = FindPart(_playerCamera.ViewportPointToRay(pointerViewport));
        if (part != null)
        {
            BeginDragPart(part, pointerViewport);
        }
    }

    private void OnClickCanceled(InputAction.CallbackContext context)
    {
        StopDrag();
    }
    private void OnExitPerformed(InputAction.CallbackContext context)
    {
        _currentPlayer.OnUnPossessionState(this);
    }

    private void GenerateParts()
    {
        IReadOnlyList<int> topAmperages = null;
        IReadOnlyList<int> coreAmperages = null;
        IReadOnlyList<int> bottomAmperages = null;

        if (fuseRecipeCatalog != null)
        {
            topAmperages = fuseRecipeCatalog.TopPartAmperages;
            coreAmperages = fuseRecipeCatalog.CorePartAmperages;
            bottomAmperages = fuseRecipeCatalog.BottomPartAmperages;
        }

        SpawnPartGroup(topPartPrefab, topPartSpawnPoints, topPartLabels, topAmperages);
        SpawnPartGroup(corePartPrefab, corePartSpawnPoints, corePartLabels, coreAmperages);
        SpawnPartGroup(bottomPartPrefab, bottomPartSpawnPoints, bottomPartLabels, bottomAmperages);

    }

    private void SpawnPartGroup(
        FusePart partPrefab, List<Transform> spawnPoints, List<TMP_Text> labels, IReadOnlyList<int> catalogAmperages)
    {
        if (partPrefab == null)
        {
            Log.Warning("[FuseWorkbench] Part prefab is missing");
            return;
        }

        int amountToSpawn = spawnPoints.Count;
        if (catalogAmperages != null)
        {
            amountToSpawn = Mathf.Min(amountToSpawn, catalogAmperages.Count);
        }

        if (amountToSpawn == 0)
        {
            Log.Warning("[FuseWorkbench] Spawn points missing or no amperages provided");
            return;
        }

        for (int i = 0; i < amountToSpawn; i++)
        {
            Transform spawnPoint = spawnPoints[i];

            if (spawnPoint == null)
            {
                continue;
            }

            FusePart spawnedPart = Instantiate(partPrefab, spawnPoint.position, spawnPoint.rotation);
            spawnedPart.SnapTo(spawnPoint);
            
            if (catalogAmperages != null)
            {
                spawnedPart.SetAmperage(catalogAmperages[i]);
            }

            if (i < labels.Count && labels[i] != null)
            {
                labels[i].raycastTarget = false;
                labels[i].text = $"{spawnedPart.Amperage}A";
            }

            spawnedPart.CacheInitialPlacement();
            _spawnedParts.Add(spawnedPart);
        }
    }

    private FusePart FindPart(Ray ray)
    {
        Physics.SyncTransforms();
        FusePart closestPart = null;
        float closestDistance = raycastDistance;

        foreach (FusePart part in _spawnedParts)
        {
            // Query each trigger directly so the workbench's collider cannot intercept the click.
            if (part != null && part.PickCollider.Raycast(ray, out RaycastHit hit, closestDistance))
            {
                closestPart = part;
                closestDistance = hit.distance;
            }
        }

        return closestPart;
    }

    private void BeginDragPart(FusePart part, Vector2 pointerViewport)
    {
        Transform snapPoint = GetSnapPointForPart(part);
        if (_draggedPart != null || snapPoint == null)
        {
            return;
        }

        _draggedPart = part;
        _dragViewportOffset = (Vector2)_playerCamera.WorldToViewportPoint(part.transform.position) - pointerViewport;
        part.SetSelected(true);
        DetachPartFromAssembly(part);
        _dragCoroutine = StartCoroutine(DragPart(part, snapPoint));
    }

    private IEnumerator DragPart(FusePart part, Transform snapPoint)
    {
        Vector3 normal = WorkPlaneNormal;
        float pickupHeight = Vector3.Dot(part.transform.position, normal);
        float hoverHeight = Vector3.Dot(snapPoint.position, normal) + dragSurfaceOffset;
        float height = pickupHeight;
        Quaternion pickupRotation = part.transform.rotation;
        float approachRadius = partSnapRadius * 1.75f;

        // Start in the pickup pose, then lower toward the table as the cursor enters the crafting area.
        while (true)
        {
            yield return null;
            if (!TryGetDragRay(out Ray ray))
            {
                continue;
            }

            bool approaching = IsNearSnapPoint(ray, snapPoint, approachRadius);
            height = Mathf.MoveTowards(height, approaching ? hoverHeight : pickupHeight, DragHeightSpeed * Time.deltaTime);
            Plane dragPlane = new Plane(normal, -height);
            if (dragPlane.Raycast(ray, out float distance) && distance <= raycastDistance)
            {
                Quaternion targetRotation = approaching ? snapPoint.rotation : pickupRotation;
                Quaternion rotation = Quaternion.RotateTowards(
                    part.transform.rotation, targetRotation, DragRotationSpeed * Time.deltaTime);
                part.transform.SetPositionAndRotation(ray.GetPoint(distance), rotation);
            }
        }
    }

    private bool TryGetDragRay(out Ray ray)
    {
        ray = default;
        if (_playerCamera == null || !TryGetPointerViewport(out Vector2 pointerViewport))
        {
            return false;
        }

        ray = _playerCamera.ViewportPointToRay(pointerViewport + _dragViewportOffset);
        return true;
    }

    private static bool TryGetPointerViewport(out Vector2 pointerViewport)
    {
        pointerViewport = default;
        if (Mouse.current == null)
        {
            return false;
        }

        // The camera's render texture is stretched across the game window.
        Vector2 pointer = Mouse.current.position.ReadValue();
        pointerViewport = new Vector2(pointer.x / Screen.width, pointer.y / Screen.height);
        return true;
    }

    private bool IsNearSnapPoint(Ray ray, Transform snapPoint, float radius)
    {
        Plane snapPlane = new Plane(WorkPlaneNormal, snapPoint.position);
        return snapPlane.Raycast(ray, out float distance)
            && distance <= raycastDistance
            && (ray.GetPoint(distance) - snapPoint.position).sqrMagnitude <= radius * radius;
    }

    private void StopDrag()
    {
        if (_dragCoroutine != null)
        {
            StopCoroutine(_dragCoroutine);
            _dragCoroutine = null;
        }

        FusePart part = _draggedPart;
        _draggedPart = null;
        if (part == null)
        {
            return;
        }

        Transform snapPoint = GetSnapPointForPart(part);
        if (snapPoint != null && TryGetDragRay(out Ray ray) && IsNearSnapPoint(ray, snapPoint, partSnapRadius))
        {
            SnapPart(part, snapPoint);
        }
        else
        {
            part.ReturnToInitialPlacement();
        }
    }

    private Transform GetSnapPointForPart(FusePart part)
    {
        switch (part.PartType)
        {
            case FusePartType.Top:
                return topAssemblyPoint;
            case FusePartType.Core:
                return coreAssemblyPoint;
            case FusePartType.Bottom:
                return bottomAssemblyPoint;
            default:
                Log.Warning("[FuseWorkbench] Unsupported fuse part type");
                return null;
        }
    }

    private void SnapPart(FusePart part, Transform snapPoint)
    {
        switch (part.PartType)
        {
            case FusePartType.Top:
                SetSnappedPart(ref _snappedTopPart, part, snapPoint);
                _isTopConnectionSoldered = false;
                break;
            case FusePartType.Core:
                SetSnappedPart(ref _snappedCorePart, part, snapPoint);
                _isTopConnectionSoldered = false;
                _isBottomConnectionSoldered = false;
                break;
            case FusePartType.Bottom:
                SetSnappedPart(ref _snappedBottomPart, part, snapPoint);
                _isBottomConnectionSoldered = false;
                break;
            default:
                Log.Warning("[FuseWorkbench] Unsupported fuse part type");
                break;
        }
    }

    private void SetSnappedPart(ref FusePart snappedPart, FusePart newPart, Transform snapPoint)
    {
        if (snappedPart != null && snappedPart != newPart)
        {
            snappedPart.ReturnToInitialPlacement();
        }

        snappedPart = newPart;
        snappedPart.SnapTo(snapPoint);
        snappedPart.SetSelected(true);
    }

    private void DetachPartFromAssembly(FusePart part)
    {
        bool wasSnapped = false;

        if (part == _snappedTopPart)
        {
            _snappedTopPart = null;
            _isTopConnectionSoldered = false;
            wasSnapped = true;
        }
        else if (part == _snappedCorePart)
        {
            _snappedCorePart = null;
            _isTopConnectionSoldered = false;
            _isBottomConnectionSoldered = false;
            wasSnapped = true;
        }
        else if (part == _snappedBottomPart)
        {
            _snappedBottomPart = null;
            _isBottomConnectionSoldered = false;
            wasSnapped = true;
        }

        if (wasSnapped)
        {
            CancelHandheldSoldering();
        }
    }

    private IEnumerator SolderWithHandheldBlowtorch(FuseWorkbenchConnectionType target)
    {
        SFXManager.PostEvent(blowtorchStartEvent, gameObject);
        yield return new WaitForSeconds(solderDuration);

        CompleteSolderTarget(target);
        EndHandheldSoldering();
        TryAssembleFuse();
    }

    private void EndHandheldSoldering()
    {
        _handheldSolderCoroutine = null;
        if (_solderingTorch != null)
        {
            _solderingTorch.EndFuseSoldering();
        }
        _solderingTorch = null;
        SFXManager.PostEvent(blowtorchStopEvent, gameObject);
    }

    private FuseWorkbenchConnectionType GetNextPendingSolderTarget()
    {
        if (_snappedCorePart == null) return FuseWorkbenchConnectionType.None;

        if (!_isTopConnectionSoldered && _snappedTopPart != null)
        {
            return FuseWorkbenchConnectionType.TopToCore;
        }

        if (!_isBottomConnectionSoldered && _snappedBottomPart != null)
        {
            return FuseWorkbenchConnectionType.BottomToCore;
        }

        return FuseWorkbenchConnectionType.None;
    }

    private void CompleteSolderTarget(FuseWorkbenchConnectionType solderTarget)
    {
        switch (solderTarget)
        {
            case FuseWorkbenchConnectionType.TopToCore:
                _isTopConnectionSoldered = true;
                Log.Info("[FuseWorkbench] Top connection soldered.");
                break;
            case FuseWorkbenchConnectionType.BottomToCore:
                _isBottomConnectionSoldered = true;
                Log.Info("[FuseWorkbench] Bottom connection soldered.");
                break;
        }
    }

    private void TryAssembleFuse()
    {
        if (_snappedTopPart == null || _snappedCorePart == null || _snappedBottomPart == null) return;
        if (!_isTopConnectionSoldered || !_isBottomConnectionSoldered) return;
        if (assembledFusePrefab == null || assembledFuseSpawnPoint == null)
        {
            Log.Warning("[FuseWorkbench] Assembled Fuse Prefab or Spawn Point Not Set");
            return;
        }
        int totalAmperage = _snappedTopPart.Amperage + _snappedCorePart.Amperage + _snappedBottomPart.Amperage;
        Fuse newFuse = Instantiate(assembledFusePrefab, assembledFuseSpawnPoint.position, assembledFuseSpawnPoint.rotation);
        newFuse.SetAmperage(totalAmperage);
        newFuse.Restore();

        if (destroyPreviousFuseOnAssembly && _assembledFuse != null && !_assembledFuse.HasBeenPickedUp)
        {
            Destroy(_assembledFuse.gameObject);
        }

        _assembledFuse = newFuse;
        ReturnAssemblyPartsToStock();
        Log.Info($"Fuse assembled with {totalAmperage}A.");
    }

    private void ReturnAssemblyPartsToStock()
    {
        _snappedTopPart.ReturnToInitialPlacement();
        _snappedCorePart.ReturnToInitialPlacement();
        _snappedBottomPart.ReturnToInitialPlacement();
        _snappedTopPart = null;
        _snappedCorePart = null;
        _snappedBottomPart = null;
        _isTopConnectionSoldered = false;
        _isBottomConnectionSoldered = false;
    }
}
