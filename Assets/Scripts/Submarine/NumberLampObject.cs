using UnityEngine;

public class NumberLampObject : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private int desiredNumber;
    
    private MaterialPropertyBlock _lightMaterialPropertyBlock;

    public void Initialize()
    {
        _lightMaterialPropertyBlock = new MaterialPropertyBlock();
    }
    
    private void ApplySettings()
    {
        if (targetRenderer != null)
        {
            targetRenderer.GetPropertyBlock(_lightMaterialPropertyBlock);
            _lightMaterialPropertyBlock.SetFloat("_TileNumber", desiredNumber);
            targetRenderer.SetPropertyBlock(_lightMaterialPropertyBlock);
        }
    }

    public void ChangeNumber(int number)
    {
        desiredNumber = number;
        ApplySettings();
    }   
}
