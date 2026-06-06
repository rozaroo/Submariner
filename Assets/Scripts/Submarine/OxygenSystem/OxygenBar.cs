using UnityEngine;

// Asignar el rectángulo 3D en barTransform — se achicará en el eje X conforme baje el oxígeno.
public class OxygenBar : MonoBehaviour
{
    private Transform _barTransform;
    private Vector3 _originalScale;
    private Vector3 _originalPosition;

    private void Awake()
    {
        _barTransform = gameObject.transform;
        _originalScale    = _barTransform.localScale;
        _originalPosition = _barTransform.localPosition;
    }

    private void OnEnable()
    {
        GameEventChannel<OnOxygenChanged>.OnEventRaised += UpdateBar;
    }

    private void OnDisable()
    {
        GameEventChannel<OnOxygenChanged>.OnEventRaised -= UpdateBar;
    }

    private void UpdateBar(OnOxygenChanged oxygenProperty)
    {
        float ratio = Mathf.Clamp01(oxygenProperty.currentOxygen / oxygenProperty.maxOxygen);

        _barTransform.localScale = new Vector3(
            _originalScale.x * ratio,
            _originalScale.y,
            _originalScale.z
        );

        _barTransform.localPosition = new Vector3(
            _originalPosition.x - (_originalScale.x * (1f - ratio)) / 2f,
            _originalPosition.y,
            _originalPosition.z
        );
    }
}
