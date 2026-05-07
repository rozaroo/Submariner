using UnityEngine;

// Attach este script en cualquier GameObject de la escena.
// Asignar el rectángulo 3D en barTransform — se achicará en el eje X conforme baje el oxígeno.
public class OxygenBar : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] private Transform barTransform;
    
    [Header("Event Channels")]
    public FloatEventChannelSO onSuffocationProgress;
    public OxygenPropertyEventSO onOxygenChanged;

    private Vector3 _originalScale;
    private Vector3 _originalPosition;

    private void Awake()
    {
        if(onSuffocationProgress  == null) Log.Error("on Suffocation Progress Event Not placed");
        if(onOxygenChanged  == null) Log.Error("On Oxygen Changed Event Not placed");
        _originalScale    = barTransform.localScale;
        _originalPosition = barTransform.localPosition;
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
