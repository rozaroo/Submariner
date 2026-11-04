using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Fuse : MonoBehaviour, IInteractable, IPickable
{
    private const int IGNORE_RAYCAST_LAYER = 2;

    [Header("Position Settings")]
    [SerializeField] private Vector3 holdPositionOffset = new Vector3(0.2f, -0.2f, 0.6f);
    [SerializeField] private Vector3 holdRotationOffset = new Vector3(0f, 0f, 0f);

    [Header("Fuse State")]
    [SerializeField] private bool isBurned = false;
    [SerializeField] private int amperage = 40;
 
    [Header("Visuals")]
    [SerializeField] private Mesh burnedMesh;
    [SerializeField] private TextMesh amperageLabel;
    [SerializeField] private ParticleSystem blowOutParticles;
    
    [SerializeField] private string _fuseBreakSFX = "Fuse_Break_SFX";
    
    [Header("Interaction")]
    [SerializeField] private InteractionUIData interactionUIData;

    private Collider _collider;
    private Rigidbody _rb;
    private MeshFilter _meshFilter;
    private bool _isInstalled;

    public GameObject GameObject => gameObject;
    public Vector3 HoldPositionOffset => holdPositionOffset;
    public Vector3 HoldRotationOffset => holdRotationOffset;
    public bool IsBurned => isBurned;
    public bool IsFunctional => !isBurned;
    public int Amperage => amperage;
    internal bool HasBeenPickedUp { get; private set; }

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
        if (_isInstalled)
        {
            return;
        }

        player.InventorySystem.TryPickUp(this);
    }

    public InteractionFeedback SendInteractionFeedback(PlayerCharacter player)
    {
        if (_isInstalled)
        {
            return new InteractionFeedback("", interactionUIData, false);
        }

        if (player != null && player.InventorySystem != null)
        {
            if (player.InventorySystem.HeldItem != null)
            {
                return new InteractionFeedback("Holding Another Item", interactionUIData);
            }
        }
        return new InteractionFeedback("Pick up Fuse", interactionUIData);
    }

    public void OnPickUp()
    {
        HasBeenPickedUp = true;
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
        SFXManager.PostEvent(_fuseBreakSFX, gameObject);
        if (blowOutParticles != null)
        {
            blowOutParticles.Play();
        }
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

    public void InstallInPanel()
    {
        _isInstalled = true;
        _rb.isKinematic = true;
        _collider.enabled = true;
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
