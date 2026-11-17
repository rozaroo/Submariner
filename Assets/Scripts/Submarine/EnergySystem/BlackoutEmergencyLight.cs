using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public class BlackoutEmergencyLight : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Sistema de energia. Si no se asigna, se busca una vez al iniciar.")]
    [SerializeField] private EnergySystem energySystem;

    [Tooltip("Unica luz de emergencia. Mantener su GameObject activo y fuera de los gestores de luces normales.")]
    [SerializeField] private Light emergencyLight;

    private void Awake()
    {
        if (energySystem == null)
        {
            energySystem = FindFirstObjectByType<EnergySystem>();
        }

        if (emergencyLight == null)
        {
            emergencyLight = GetComponent<Light>();
        }
    }

    private void OnEnable()
    {
        GameEventChannel<OnEnergyStatusChange>.OnEventRaised += OnEnergyStatusChanged;
        RefreshLightState();
    }

    private void Start()
    {
        // Se ejecuta despues de la inicializacion de EnergySystem.
        RefreshLightState();
    }

    private void OnDisable()
    {
        GameEventChannel<OnEnergyStatusChange>.OnEventRaised -= OnEnergyStatusChanged;
        SetLightEnabled(false);
    }

    private void OnEnergyStatusChanged(OnEnergyStatusChange status)
    {
        SetLightEnabled(status.energyStatus == EnergyStatus.Empty);
    }

    private void RefreshLightState()
    {
        bool isBlackout = energySystem != null
            && (energySystem.IsFuseBroken || energySystem.GetCurrentEnergy() <= 0f);

        SetLightEnabled(isBlackout);
    }

    private void SetLightEnabled(bool isEnabled)
    {
        if (emergencyLight != null)
        {
            emergencyLight.enabled = isEnabled;
        }
    }
}
