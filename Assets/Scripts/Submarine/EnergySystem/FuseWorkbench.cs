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

    [Header("Soldering")]
    [SerializeField] private FuseSolderingIron solderingIronPrefab;
    [SerializeField] private FuseSolderingIron solderingIron;
    [SerializeField] private Transform solderingIronRestPoint;
    [SerializeField] private bool generateSolderingIronIfMissing = true;
    [SerializeField] private Transform topConnectionPoint;
    [SerializeField] private Transform bottomConnectionPoint;
    [SerializeField] private float solderRadius = 0.2f;
    [SerializeField] private float solderDuration = 1f;

    [Header("Top Parts")]
    [SerializeField] private List<FusePart> topPartPrefabs = new();
    [SerializeField] private List<Transform> topPartSpawnPoints = new();

    [Header("Core Parts")]
    [SerializeField] private List<FusePart> corePartPrefabs = new();
    [SerializeField] private List<Transform> corePartSpawnPoints = new();

    [Header("Bottom Parts")]
    [SerializeField] private List<FusePart> bottomPartPrefabs = new();
    [SerializeField] private List<Transform> bottomPartSpawnPoints = new();

    private readonly List<FusePart> _spawnedParts = new();

    private PlayerCharacter _currentPlayer;
    private Camera _playerCamera;
    private FusePart _snappedTopPart;
    private FusePart _snappedCorePart;
    private FusePart _snappedBottomPart;
    private FusePart _draggedPart;
    private FuseSolderingIron _draggedSolderingIron;
    private Fuse _assembledFuse;
    private Coroutine _dragCoroutine;
    private Coroutine _hoverCoroutine;
    private FusePart _hoveredPart;
    private FuseWorkbenchConnectionType _solderTarget;
    private float _solderProgress;
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
        player.OnPossessionState(this);
    }
    
    public void Possess(PlayerCharacter playerCharacter)
    {
        _currentPlayer = playerCharacter;
        _playerCamera = playerCharacter.camController.MainCamera;
        
        InputAction clickAction = _currentPlayer.Input.actions[clickActionName];
        InputAction exitAction = _currentPlayer.Input.actions[exitActionName];
        
        clickAction.started += OnClickStarted;
        clickAction.canceled += OnClickCanceled;
        exitAction.started += OnExitPerformed;

        if (!_hasGeneratedParts)
        {
            GenerateParts();
        }
        EnsureSolderingIron();
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
        Ray ray = _playerCamera.ScreenPointToRay(mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
        {
            return;
        }

        FusePart fusePart = hit.collider.GetComponentInParent<FusePart>();
        if (fusePart != null)
        {
            SetHoveredPart(null);
            BeginDragPart(fusePart);
            return;
        }

        FuseSolderingIron hitSolderingIron = hit.collider.GetComponentInParent<FuseSolderingIron>();
        if (hitSolderingIron != null && hitSolderingIron == solderingIron)
        {
            SetHoveredPart(null);
            BeginDragSolderingIron(hitSolderingIron);
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

        SpawnPartGroup(topPartPrefabs, topPartSpawnPoints, topAmperages);
        SpawnPartGroup(corePartPrefabs, corePartSpawnPoints, coreAmperages);
        SpawnPartGroup(bottomPartPrefabs, bottomPartSpawnPoints, bottomAmperages);

        _hasGeneratedParts = true;
    }

    private void EnsureSolderingIron()
    {
        if (solderingIron != null)
        {
            return;
        }

        if (!generateSolderingIronIfMissing)
        {
            return;
        }

        if (solderingIronPrefab == null || solderingIronRestPoint == null)
        {
            Log.Warning("[FuseWorkbench] Soldering Iron Prefab or Rest Point Not Set");
            return;
        }

        solderingIron = Instantiate(
            solderingIronPrefab,
            solderingIronRestPoint.position,
            solderingIronRestPoint.rotation,
            transform
        );

        solderingIron.SnapTo(solderingIronRestPoint);
        solderingIron.CacheInitialPlacement();
    }

    private void SpawnPartGroup(List<FusePart> partPrefabs, List<Transform> spawnPoints, IReadOnlyList<int> catalogAmperages)
    {
        int amountToSpawn = Mathf.Min(partPrefabs.Count, spawnPoints.Count);
        if (catalogAmperages != null)
        {
            amountToSpawn = Mathf.Min(amountToSpawn, catalogAmperages.Count);
        }

        if (amountToSpawn == 0)
        {
            Log.Warning("[FuseWorkbench] Part prefabs or spawn points missing");
            return;
        }

        for (int i = 0; i < amountToSpawn; i++)
        {
            FusePart partPrefab = partPrefabs[i];
            Transform spawnPoint = spawnPoints[i];

            if (partPrefab == null || spawnPoint == null)
            {
                continue;
            }

            FusePart spawnedPart = Instantiate(partPrefab, spawnPoint.position, spawnPoint.rotation, transform);
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

    private void BeginDragSolderingIron(FuseSolderingIron hitSolderingIron)
    {
        StopDrag();
        SetHoveredPart(null);
        _draggedSolderingIron = hitSolderingIron;
        _draggedSolderingIron.SetSelected(true);
        _draggedSolderingIron.transform.SetParent(transform, true);
        _solderTarget = FuseWorkbenchConnectionType.None;
        _solderProgress = 0f;
        _dragCoroutine = StartCoroutine(DragSolderingIron());
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

    private IEnumerator DragSolderingIron()
    {
        while (_draggedSolderingIron != null)
        {
            if (TryGetCursorPointOnWorkPlane(out Vector3 cursorPoint))
            {
                _draggedSolderingIron.transform.position = cursorPoint;
                HandleSoldering(cursorPoint);
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
            if (_draggedPart != null || _draggedSolderingIron != null)
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
        Ray ray = _playerCamera.ScreenPointToRay(mousePosition);

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
        Ray ray = _playerCamera.ScreenPointToRay(mousePosition);
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

        if (_draggedSolderingIron != null)
        {
            DropSolderingIron();
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

    private void DropSolderingIron()
    {
        _draggedSolderingIron.SetSelected(false);
        _draggedSolderingIron.SetSoldering(false);

        if (solderingIronRestPoint != null)
        {
            _draggedSolderingIron.SnapTo(solderingIronRestPoint);
        }
        else
        {
            _draggedSolderingIron.ReturnToInitialPlacement();
        }

        _draggedSolderingIron = null;
        _solderTarget = FuseWorkbenchConnectionType.None;
        _solderProgress = 0f;
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

    private void HandleSoldering(Vector3 solderingPosition)
    {
        FuseWorkbenchConnectionType newTarget = GetSolderTarget(solderingPosition);
        if (newTarget == FuseWorkbenchConnectionType.None)
        {
            _solderTarget = FuseWorkbenchConnectionType.None;
            _solderProgress = 0f;
            _draggedSolderingIron.SetSoldering(false);
            return;
        }

        if (newTarget != _solderTarget)
        {
            _solderTarget = newTarget;
            _solderProgress = 0f;
        }

        _draggedSolderingIron.SetSoldering(true);
        _solderProgress += Time.deltaTime;

        if (_solderProgress < solderDuration)
        {
            return;
        }

        CompleteSolderTarget(_solderTarget);
        _solderProgress = 0f;
        TryAssembleFuse();
    }

    private FuseWorkbenchConnectionType GetSolderTarget(Vector3 solderingPosition)
    {
        if (_snappedCorePart == null)
        {
            return FuseWorkbenchConnectionType.None;
        }

        if (!_isTopConnectionSoldered && _snappedTopPart != null && topConnectionPoint != null)
        {
            float topDistance = Vector3.Distance(solderingPosition, topConnectionPoint.position);
            if (topDistance <= solderRadius)
            {
                return FuseWorkbenchConnectionType.TopToCore;
            }
        }

        if (!_isBottomConnectionSoldered && _snappedBottomPart != null && bottomConnectionPoint != null)
        {
            float bottomDistance = Vector3.Distance(solderingPosition, bottomConnectionPoint.position);
            if (bottomDistance <= solderRadius)
            {
                return FuseWorkbenchConnectionType.BottomToCore;
            }
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
        if (_snappedTopPart == null || _snappedCorePart == null || _snappedBottomPart == null)
        {
            return;
        }

        if (!_isTopConnectionSoldered || !_isBottomConnectionSoldered)
        {
            return;
        }

        if (assembledFusePrefab == null || assembledFuseSpawnPoint == null)
        {
            Log.Warning("[FuseWorkbench] Assembled Fuse Prefab or Spawn Point Not Set");
            return;
        }

        if (destroyPreviousFuseOnAssembly && _assembledFuse != null)
        {
            Destroy(_assembledFuse.gameObject);
        }

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
