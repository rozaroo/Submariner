using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class FuseWorkbench : MonoBehaviour, IInteractable, IPossessable
{
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
    
    [Header("Audio (Wwise)")]
    [SerializeField] private string blowtorchStartEvent = "Start_FuseMake";
    [SerializeField] private string blowtorchStopEvent = "Stop_FuseMake";
    [SerializeField] private float solderDuration = 1f;

    [Header("Top Parts")]
    [SerializeField] private FusePart topPartPrefab;
    [SerializeField] private List<Transform> topPartSpawnPoints = new();

    [Header("Core Parts")]
    [SerializeField] private FusePart corePartPrefab;
    [SerializeField] private List<Transform> corePartSpawnPoints = new();

    [Header("Bottom Parts")]
    [SerializeField] private FusePart bottomPartPrefab;
    [SerializeField] private List<Transform> bottomPartSpawnPoints = new();

    private readonly List<FusePart> _spawnedParts = new();

    private PlayerCharacter _currentPlayer;
    private Camera _playerCamera;
    private FusePart _snappedTopPart;
    private FusePart _snappedCorePart;
    private FusePart _snappedBottomPart;
    private FusePart _draggedPart;
    private Fuse _assembledFuse;
    private Coroutine _dragCoroutine;
    private Coroutine _hoverCoroutine;
    private Coroutine _handheldSolderCoroutine;
    private FusePart _hoveredPart;
    private bool _hasGeneratedParts;
    private bool _isTopConnectionSoldered;
    private bool _isBottomConnectionSoldered;

    public string MapName => stationMapName;
    public Transform CameraAnchor => cameraAnchor;
    public Transform DirectionAnchor => directionAnchor;
    public float TransitionDuration => transitionDuration;
    public CursorLockMode CursorLockMode => cursorLockMode;
    public bool IsMouseVisible => showMouseCursor;

    public void Interact(PlayerCharacter player)
    {
        // With the blowtorch equipped, interacting with the bench welds the next
        // pending joint instead of entering the part-placement view.
        if (player.InventorySystem.IsHolding<Blowtorch>())
        {
            TrySolderWithHandheldBlowtorch();
            return;
        }

        player.OnPossessionState(this);
    }

    /// <summary>
    /// Starts one solder operation using the player's equipped blowtorch.
    /// The bench keeps ownership of the FuseMake audio because it is the
    /// object that knows when a valid connection has completed.
    /// </summary>
    public bool TrySolderWithHandheldBlowtorch()
    {
        if (!_hasGeneratedParts)
        {
            GenerateParts();
        }

        if (_handheldSolderCoroutine != null)
        {
            return true;
        }

        FuseWorkbenchConnectionType target = GetNextPendingSolderTarget();
        if (target == FuseWorkbenchConnectionType.None)
        {
            return false;
        }

        enabled = true;
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
        _handheldSolderCoroutine = null;
        SFXManager.PostEvent(blowtorchStopEvent, gameObject);
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

        if (!_hasGeneratedParts)
        {
            GenerateParts();
        }
        StartHoverTracking();
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
        StopHoverTracking();
        _currentPlayer = null;
        _playerCamera = null;
        enabled = false;
    }

    private void OnClickStarted(InputAction.CallbackContext context)
    {
        if (Mouse.current == null || _playerCamera == null)
        {
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        Vector2 viewportPos = new Vector2(
            mousePosition.x / Screen.width,
            mousePosition.y / Screen.height
        );

        Ray ray = _playerCamera.ViewportPointToRay(viewportPos);

        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
        {
            FusePart fusePart = hit.collider.GetComponentInParent<FusePart>();

            if (fusePart != null)
            {
                SetHoveredPart(null);
                BeginDragPart(fusePart);
                return;
            }
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

        SpawnPartGroup(topPartPrefab, topPartSpawnPoints, topAmperages);
        SpawnPartGroup(corePartPrefab, corePartSpawnPoints, coreAmperages);
        SpawnPartGroup(bottomPartPrefab, bottomPartSpawnPoints, bottomAmperages);

        _hasGeneratedParts = true;
    }

    private void SpawnPartGroup(FusePart partPrefab, List<Transform> spawnPoints, IReadOnlyList<int> catalogAmperages)
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

            spawnedPart.CacheInitialPlacement();
            _spawnedParts.Add(spawnedPart);
        }
    }

    private void BeginDragPart(FusePart fusePart)
    {
        StopDrag();
        SetHoveredPart(null);
        _draggedPart = fusePart;
        _draggedPart.SetSelected(true);
        DetachPartFromAssembly(_draggedPart);
        _draggedPart.transform.SetParent(transform, true);
        _dragCoroutine = StartCoroutine(DragPart());
    }

    private IEnumerator DragPart()
    {
        while (_draggedPart != null)
        {
            if (TryGetCursorPointOnWorkPlane(out Vector3 cursorPoint))
            {
                _draggedPart.transform.position = cursorPoint;
            }

            yield return null;
        }
    }

    private void StartHoverTracking()
    {
        if (_hoverCoroutine != null)
        {
            StopCoroutine(_hoverCoroutine);
        }

        _hoverCoroutine = StartCoroutine(TrackHoveredPart());
    }

    private void StopHoverTracking()
    {
        if (_hoverCoroutine != null)
        {
            StopCoroutine(_hoverCoroutine);
            _hoverCoroutine = null;
        }

        SetHoveredPart(null);
    }

    private IEnumerator TrackHoveredPart()
    {
        while (true)
        {
            if (_draggedPart != null)
            {
                SetHoveredPart(null);
                yield return null;
                continue;
            }

            SetHoveredPart(GetPartUnderCursor());

            if (_hoveredPart != null)
            {
                _hoveredPart.UpdateHoverLabel(_playerCamera);
            }

            yield return null;
        }
    }

    private FusePart GetPartUnderCursor()
    {
        if (Mouse.current == null || _playerCamera == null)
        {
            return null;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector2 viewportPos = new Vector2(mousePosition.x / Screen.width, mousePosition.y / Screen.height);
        Ray ray = _playerCamera.ViewportPointToRay(viewportPos);

        if (!Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
        {
            return null;
        }

        return hit.collider.GetComponentInParent<FusePart>();
    }

    private void SetHoveredPart(FusePart newHoveredPart)
    {
        if (_hoveredPart == newHoveredPart)
        {
            return;
        }

        if (_hoveredPart != null)
        {
            _hoveredPart.SetHovered(false, null);
        }

        _hoveredPart = newHoveredPart;

        if (_hoveredPart != null)
        {
            _hoveredPart.SetHovered(true, _playerCamera);
        }
    }

    private bool TryGetCursorPointOnWorkPlane(out Vector3 cursorPoint)
    {
        cursorPoint = Vector3.zero;

        if (Mouse.current == null || _playerCamera == null)
        {
            return false;
        }

        Transform planeAnchor = transform;
        if (workPlaneAnchor != null)
        {
            planeAnchor = workPlaneAnchor;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector2 viewportPos = new Vector2(mousePosition.x / Screen.width, mousePosition.y / Screen.height);
        Ray ray = _playerCamera.ViewportPointToRay(viewportPos);
        
        Plane workPlane = new Plane(planeAnchor.up, planeAnchor.position);

        if (!workPlane.Raycast(ray, out float distance))
        {
            return false;
        }

        cursorPoint = ray.GetPoint(distance) + (planeAnchor.up * dragSurfaceOffset);
        return true;
    }

    private void StopDrag()
    {
        if (_dragCoroutine != null)
        {
            StopCoroutine(_dragCoroutine);
            _dragCoroutine = null;
        }
        if (_draggedPart != null)
        {
            DropDraggedPart();
        }
    }

    private void DropDraggedPart()
    {
        FusePart part = _draggedPart;
        _draggedPart = null;

        if (TrySnapPartToAssembly(part))
        {
            return;
        }

        part.ReturnToInitialPlacement();
        DestroyAssembledFuse();
    }

    private bool TrySnapPartToAssembly(FusePart part)
    {
        Transform snapPoint = GetSnapPointForPart(part);
        if (snapPoint == null)
        {
            Log.Warning("[FuseWorkbench] Assembly snap point missing");
            return false;
        }

        float distanceToSnap = Vector3.Distance(part.transform.position, snapPoint.position);
        if (distanceToSnap > partSnapRadius)
        {
            part.SetSelected(false);
            return false;
        }

        SnapPart(part, snapPoint);
        return true;
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

        DestroyAssembledFuse();
        TryAssembleFuse();
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
            DestroyAssembledFuse();
        }
    }

    private IEnumerator SolderWithHandheldBlowtorch(FuseWorkbenchConnectionType target)
    {
        SFXManager.PostEvent(blowtorchStartEvent, gameObject);
        yield return new WaitForSeconds(solderDuration);

        CompleteSolderTarget(target);
        _handheldSolderCoroutine = null;
        TryAssembleFuse();
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
        SFXManager.PostEvent(blowtorchStopEvent, gameObject);
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
        if (destroyPreviousFuseOnAssembly && _assembledFuse != null) Destroy(_assembledFuse.gameObject);
        int totalAmperage = _snappedTopPart.Amperage + _snappedCorePart.Amperage + _snappedBottomPart.Amperage;
        _assembledFuse = Instantiate(assembledFusePrefab, assembledFuseSpawnPoint.position, assembledFuseSpawnPoint.rotation);
        _assembledFuse.SetAmperage(totalAmperage);
        _assembledFuse.Restore();

        Log.Info($"Fuse assembled with {totalAmperage}A.");
    }

    private void DestroyAssembledFuse()
    {
        if (_assembledFuse == null)
        {
            return;
        }

        Destroy(_assembledFuse.gameObject);
        _assembledFuse = null;
    }
}
