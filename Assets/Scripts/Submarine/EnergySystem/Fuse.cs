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
    [SerializeField] private GameObject functionalVisual;
    [SerializeField] private GameObject burnedVisual;
    [SerializeField] private TextMesh amperageLabel;

    [Header("Amperage Label")]
    [SerializeField] private bool showAmperageLabel = true;
    [SerializeField] private bool createAmperageLabelIfMissing = true;
    [SerializeField] private Vector3 amperageLabelLocalOffset = Vector3.zero;
    [SerializeField] private Vector3 amperageLabelLocalRotation = Vector3.zero;
    [SerializeField] private float amperageLabelCharacterSize = 0.08f;
    [SerializeField] private Color amperageLabelColor = Color.white;

    private Collider _collider;
    private Rigidbody _rb;
    private EnergyPanelControl _installedPanel;

    public GameObject GameObject => gameObject;
    public Vector3 HoldPositionOffset => holdOffset;
    public bool IsBurned => isBurned;
    public bool IsFunctional => !isBurned;
    public int Amperage => amperage;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        EnsureAmperageLabel();
        RefreshVisuals();
    }

    public void Interact(PlayerCharacter player)
    {
        if (_installedPanel != null)
        {
            _installedPanel.TryRemoveFuse(this, player);
            return;
        }

        player.inventorySystem.TryPickUp(this);
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
        RefreshAmperageLabel();
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
 
    private void RefreshVisuals()
    {
        if (functionalVisual != null)
        {
            functionalVisual.SetActive(!isBurned);
        }

        if (burnedVisual != null)
        {
            burnedVisual.SetActive(isBurned);
        }

        RefreshAmperageLabel();
    }

    private void RefreshAmperageLabel()
    {
        EnsureAmperageLabel();

        if (amperageLabel != null)
        {
            amperageLabel.gameObject.SetActive(showAmperageLabel);
            amperageLabel.text = $"{amperage}A";
        }
    }

    private void EnsureAmperageLabel()
    {
        if (!showAmperageLabel)
        {
            if (amperageLabel != null)
            {
                amperageLabel.gameObject.SetActive(false);
            }

            return;
        }

        if (amperageLabel == null && createAmperageLabelIfMissing)
        {
            GameObject labelObject = new GameObject("FuseAmperageLabel");
            labelObject.transform.SetParent(transform);
            amperageLabel = labelObject.AddComponent<TextMesh>();
            amperageLabel.anchor = TextAnchor.MiddleCenter;
            amperageLabel.alignment = TextAlignment.Center;
        }

        if (amperageLabel == null)
        {
            return;
        }

        amperageLabel.transform.localPosition = amperageLabelLocalOffset;
        amperageLabel.transform.localRotation = Quaternion.Euler(amperageLabelLocalRotation);
        amperageLabel.characterSize = amperageLabelCharacterSize;
        amperageLabel.color = amperageLabelColor;
        SetupLabelAsVisualOnly(amperageLabel);
    }

    private void SetupLabelAsVisualOnly(TextMesh label)
    {
        label.gameObject.layer = IGNORE_RAYCAST_LAYER;

        Collider[] labelColliders = label.GetComponentsInChildren<Collider>();
        for (int i = 0; i < labelColliders.Length; i++)
        {
            labelColliders[i].enabled = false;
        }
    }
}
