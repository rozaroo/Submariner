using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FusePart : MonoBehaviour
{
    private const int IGNORE_RAYCAST_LAYER = 2;

    [Header("Fuse Part")]
    [SerializeField] private FusePartType partType;
    [SerializeField] private int amperage = 10;

    [Header("Visuals")]
    [SerializeField] private GameObject selectedVisual;
    [SerializeField] private TextMesh amperageLabel;

    [Header("Hover Amperage")]
    [SerializeField] private bool showAmperageOnHover = true;
    [SerializeField] private bool createHoverLabelIfMissing = true;
    [SerializeField] private TextMesh hoverAmperageLabel;
    [SerializeField] private Vector3 hoverLabelLocalOffset = new Vector3(0f, 0.25f, 0f);
    [SerializeField] private float hoverLabelCharacterSize = 0.08f;
    [SerializeField] private Color hoverLabelColor = Color.white;

    private Transform _initialParent;
    private Vector3 _initialLocalPosition;
    private Quaternion _initialLocalRotation;
    private bool _hasInitialPlacement;
    private bool _isHovered;

    public FusePartType PartType => partType;
    public int Amperage => amperage;

    private void Start()
    {
        CacheInitialPlacement();
        SetSelected(false);
        RefreshAmperageLabel();
        SetHovered(false, null);
    }

    public void CacheInitialPlacement()
    {
        _initialParent = transform.parent;
        _initialLocalPosition = transform.localPosition;
        _initialLocalRotation = transform.localRotation;
        _hasInitialPlacement = true;
    }

    public void SnapTo(Transform snapPoint)
    {
        if (snapPoint == null)
        {
            return;
        }

        transform.SetParent(snapPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void ReturnToInitialPlacement()
    {
        if (!_hasInitialPlacement)
        {
            return;
        }

        transform.SetParent(_initialParent);
        transform.localPosition = _initialLocalPosition;
        transform.localRotation = _initialLocalRotation;
        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
        if (selectedVisual != null)
        {
            selectedVisual.SetActive(isSelected);
        }
    }

    public void SetHovered(bool isHovered, Camera hoverCamera)
    {
        _isHovered = isHovered && showAmperageOnHover;
        EnsureHoverLabel();

        if (hoverAmperageLabel == null)
        {
            return;
        }

        hoverAmperageLabel.gameObject.SetActive(_isHovered);
        UpdateHoverLabel(hoverCamera);
    }

    public void UpdateHoverLabel(Camera hoverCamera)
    {
        if (!_isHovered || hoverAmperageLabel == null)
        {
            return;
        }

        RefreshHoverAmperageLabel();
        FaceHoverLabelToCamera(hoverCamera);
    }

    public void SetAmperage(int newAmperage)
    {
        amperage = Mathf.Max(0, newAmperage);
        RefreshAmperageLabel();
        RefreshHoverAmperageLabel();
    }

    private void RefreshAmperageLabel()
    {
        if (amperageLabel != null)
        {
            amperageLabel.text = $"{amperage}A";
        }
    }

    private void EnsureHoverLabel()
    {
        if (!showAmperageOnHover)
        {
            return;
        }

        if (hoverAmperageLabel == null && createHoverLabelIfMissing)
        {
            GameObject labelObject = new GameObject("HoverAmperageLabel");
            labelObject.transform.SetParent(transform);
            hoverAmperageLabel = labelObject.AddComponent<TextMesh>();
            hoverAmperageLabel.anchor = TextAnchor.MiddleCenter;
            hoverAmperageLabel.alignment = TextAlignment.Center;
        }

        if (hoverAmperageLabel == null)
        {
            return;
        }

        hoverAmperageLabel.transform.localPosition = hoverLabelLocalOffset;
        hoverAmperageLabel.characterSize = hoverLabelCharacterSize;
        hoverAmperageLabel.color = hoverLabelColor;
        SetupLabelAsVisualOnly(hoverAmperageLabel);
        RefreshHoverAmperageLabel();
    }

    private void RefreshHoverAmperageLabel()
    {
        if (hoverAmperageLabel != null)
        {
            hoverAmperageLabel.text = $"{amperage}A";
        }
    }

    private void FaceHoverLabelToCamera(Camera hoverCamera)
    {
        if (hoverCamera == null)
        {
            return;
        }

        Vector3 directionToCamera = hoverAmperageLabel.transform.position - hoverCamera.transform.position;
        if (directionToCamera.sqrMagnitude <= 0.001f)
        {
            return;
        }

        hoverAmperageLabel.transform.rotation = Quaternion.LookRotation(directionToCamera.normalized, hoverCamera.transform.up);
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
