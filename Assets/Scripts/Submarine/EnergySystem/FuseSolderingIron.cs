using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FuseSolderingIron : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private GameObject selectedVisual;
    [SerializeField] private GameObject solderingVisual;

    private Transform _initialParent;
    private Vector3 _initialLocalPosition;
    private Quaternion _initialLocalRotation;
    private bool _hasInitialPlacement;

    private void Awake()
    {
        CacheInitialPlacement();
        SetSelected(false);
        SetSoldering(false);
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
    }

    public void SetSelected(bool isSelected)
    {
        if (selectedVisual != null)
        {
            selectedVisual.SetActive(isSelected);
        }
    }

    public void SetSoldering(bool isSoldering)
    {
        if (solderingVisual != null)
        {
            solderingVisual.SetActive(isSoldering);
        }
    }
}
