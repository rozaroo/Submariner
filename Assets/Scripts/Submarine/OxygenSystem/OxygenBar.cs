using UnityEngine;

// Asignar el rectángulo 3D en barTransform — se achicará en el eje X conforme baje el oxígeno.
public class OxygenBar : MonoBehaviour
{
    [Header("Event Channels")]
    public FloatEventChannelSO onSuffocationProgress;
    public OxygenPropertyEventSO onOxygenChanged;

    private Transform _barTransform;
    private Vector3 _originalScale;
    private Vector3 _originalPosition;

    private void Awake()
    {
        if(onSuffocationProgress  == null) Log.Error("on Suffocation Progress Event Not placed");
        if(onOxygenChanged  == null) Log.Error("On Oxygen Changed Event Not placed");
        _barTransform = gameObject.transform;
        _originalScale    = _barTransform.localScale;
        _originalPosition = _barTransform.localPosition;
    }

    private void OnEnable()
    {
        onOxygenChanged.OnEventRaised += UpdateBar;
    }

    private void OnDisable()
    {
        onOxygenChanged.OnEventRaised -= UpdateBar;
    }

    private void UpdateBar(OxygenProperty oxygenProperty)
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
