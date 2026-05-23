using UnityEngine;

public class Fuse : MonoBehaviour
{
    [Header("Fuse State")]
    [SerializeField] private bool isBurned = false;
 
    [Header("Visuals (opcional)")]
    [SerializeField] private GameObject functionalVisual;
    [SerializeField] private GameObject burnedVisual; //TODO: Create a burned visual for the fuse
 
    private void Start() => RefreshVisuals();
 
    public bool IsBurned => isBurned;
    public bool IsFunctional => !isBurned;
 
    public void Burn()
    {
        isBurned = true;
        RefreshVisuals();
    }
 
    private void RefreshVisuals()
    {
        if (functionalVisual != null) functionalVisual.SetActive(!isBurned);
        if (burnedVisual != null)    burnedVisual.SetActive(isBurned);
    }
}
