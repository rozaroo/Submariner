using UnityEngine;

// Attach este script en cualquier GameObject de la escena.
// Asignar el rectángulo 3D en barTransform — se achicará en el eje X conforme baje el oxígeno.
// Para que se achique desde un lado y no desde el centro, mover el pivot del objeto:
// en el modo de edición desactivar "Center" y usar "Pivot", o usar un GameObject padre como ancla.
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

    private void Update()
    {
        float ratio = Mathf.Clamp01(oxygenSystem.currentOxygen / oxygenSystem.maxOxygen);

        // Achica en X
        barTransform.localScale = new Vector3(
            _originalScale.x * ratio,
            _originalScale.y,
            _originalScale.z
        );

        // Desplaza para que se achique desde la derecha en lugar del centro
        barTransform.localPosition = new Vector3(
            _originalPosition.x - (_originalScale.x * (1f - ratio)) / 2f,
            _originalPosition.y,
            _originalPosition.z
        );
    }
}
