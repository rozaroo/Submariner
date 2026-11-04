using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FusePart : MonoBehaviour
{
    [Header("Fuse Part")]
    [SerializeField] private FusePartType partType;
    [SerializeField] private int amperage = 10;

    [Header("Visuals")]
    [SerializeField] private GameObject selectedVisual;

    private Transform _initialParent;
    private Vector3 _initialLocalPosition;
    private Quaternion _initialLocalRotation;
    private bool _hasInitialPlacement;

    public FusePartType PartType => partType;
    public int Amperage => amperage;
    public Collider PickCollider { get; private set; }

    private void Awake()
    {
        PickCollider = GetComponent<Collider>();
        SetSelected(false);
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

    public void SetAmperage(int newAmperage)
    {
        amperage = Mathf.Max(0, newAmperage);
    }
}
