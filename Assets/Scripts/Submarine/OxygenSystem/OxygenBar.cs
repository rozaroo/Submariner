using UnityEngine;

// Attach este script en cualquier GameObject de la escena.
// Asignar el rectángulo 3D en barTransform — se achicará en el eje X conforme baje el oxígeno.
public class OxygenBar : MonoBehaviour
{
    [SerializeField] private OxygenSystem oxygenSystem;
    [SerializeField] private Transform barTransform;

    private Vector3 _originalScale;
    private Vector3 _originalPosition;

    private void Awake()
    {
        _originalScale    = barTransform.localScale;
        _originalPosition = barTransform.localPosition;
    }

    private void OnEnable()
    {
        oxygenSystem.OnOxygenChanged += UpdateBar;
    }

    private void OnDisable()
    {
        oxygenSystem.OnOxygenChanged -= UpdateBar;
    }

    private void UpdateBar(float currentOxygen)
    {
        float ratio = Mathf.Clamp01(currentOxygen / oxygenSystem.MaxOxygen);

        barTransform.localScale = new Vector3(
            _originalScale.x * ratio,
            _originalScale.y,
            _originalScale.z
        );

        barTransform.localPosition = new Vector3(
            _originalPosition.x - (_originalScale.x * (1f - ratio)) / 2f,
            _originalPosition.y,
            _originalPosition.z
        );
    }
}
