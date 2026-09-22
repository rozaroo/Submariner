using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Fuse : MonoBehaviour, IInteractable, IPickable
{
    private const int IGNORE_RAYCAST_LAYER = 2;

    [Header("Position Settings")]
    [SerializeField] private Vector3 holdOffset = new Vector3(0.2f, -0.2f, 0.6f);

    [Header("Fuse State")]
    [SerializeField] private bool isBurned = false;
    [SerializeField] private int amperage = 40;
 
    [Header("Visuals")]
    [SerializeField] private Mesh burnedMesh;
    [SerializeField] private TextMesh amperageLabel;

    private Collider _collider;
    private Rigidbody _rb;
    private MeshFilter _meshFilter;
    private EnergyPanelControl _installedPanel;

    public GameObject GameObject => gameObject;
    public Vector3 HoldPositionOffset => holdOffset;
    public bool IsBurned => isBurned;
    public bool IsFunctional => !isBurned;
    public int Amperage => amperage;

    private Mesh _originalMesh;
    private bool _hasStoredOriginals;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _rb = GetComponent<Rigidbody>();
        _meshFilter = GetComponent<MeshFilter>();
    }

    private void Start()
    {
        RefreshVisuals();
    }

    public void Interact(PlayerCharacter player)
    {
        if (_installedPanel != null)
        {
            _installedPanel.TryRemoveFuse(this, player);
            return;
        }

        player.InventorySystem.TryPickUp(this);
    }

    public void OnPickUp()
    {
        _rb.isKinematic = true;
        _collider.enabled = false;
    }

    public void OnDrop()
    {
        _rb.isKinematic = false;
        _collider.enabled = true;
    }

    public void Burn()
    {
        isBurned = true;
        RefreshVisuals();
    }

    public void Restore()
    {
        isBurned = false;
        RefreshVisuals();
    }

    public void SetAmperage(int newAmperage)
    {
        amperage = Mathf.Max(0, newAmperage);
    }

    public void InstallInPanel(EnergyPanelControl panel)
    {
        _installedPanel = panel;
        _rb.isKinematic = true;
        _collider.enabled = true;
    }

    public void DetachFromPanel()
    {
        _installedPanel = null;
    }

    private void StoreOriginalVisuals()
    {
        if (_hasStoredOriginals || _meshFilter == null) return;
        
        _originalMesh = _meshFilter.sharedMesh;
        _hasStoredOriginals = true;
    }
 
    private void RefreshVisuals()
    {
        StoreOriginalVisuals();

        if (_meshFilter != null)
        {
            if (isBurned && burnedMesh != null)
            {
                _meshFilter.sharedMesh = burnedMesh;
            }
            else if (!isBurned && _originalMesh != null)
            {
                _meshFilter.sharedMesh = _originalMesh;
            }
        }
        
    }
}
