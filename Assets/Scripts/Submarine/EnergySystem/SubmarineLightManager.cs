using System.Collections;
using UnityEngine;

public class SubmarineLightManager : MonoBehaviour
{
    public static SubmarineLightManager Instance { get; private set; }

    private LightObject[] submarineLights;

    [Header("Intensity")]
    [SerializeField] private float maxIntensity = 2f;
    [SerializeField] private float minIntensity = 1.5f;
    [SerializeField] private float blackoutIntensity = 0.05f;

    [Header("Flicker")]
    [SerializeField] private bool enableFlicker = true;
    [SerializeField] private float flickerThreshold = 20f;
    [SerializeField] private float flickerChance = 0.05f;
    [SerializeField] private float minFlickerTime = 0.05f;
    [SerializeField] private float maxFlickerTime = 0.15f;

    private float currentEnergyPercentage = 100f;

    private void Awake()
    {
        Instance = this;

        submarineLights = FindObjectsByType<LightObject>(FindObjectsSortMode.None);
        submarineLights = System.Array.FindAll(submarineLights, light => light.gameObject.name == "Light");
    }

    private void OnEnable()
    {
        GameEventChannel<OnEnergyPropertyChange>.OnEventRaised += OnEnergyChanged;
    }

    private void OnDisable()
    {
        GameEventChannel<OnEnergyPropertyChange>.OnEventRaised -= OnEnergyChanged;
    }

    private void Start()
    {
        if (enableFlicker)
        {
            foreach (LightObject light in submarineLights)
                StartCoroutine(FlickerRoutine(light));
        }
    }

    private void OnEnergyChanged(OnEnergyPropertyChange data)
    {
        currentEnergyPercentage = data.currentEnergyPercentage;
    }

    public void UpdateLightIntensity()
    {
        
        if (LightManager.Instance != null && (LightManager.Instance.IsAlertActive || LightManager.Instance.IsBlackout || LightManager.Instance.IsRestoring))
        {
            return;
        }

        bool hasEnergy = currentEnergyPercentage > 0f;
        float intensity = hasEnergy ? maxIntensity : blackoutIntensity;

        foreach (LightObject light in submarineLights)
        {
            if (light != null)
            {
                light.Toggle(true);
                light.SetIntensity(intensity);
            }
        }
    }

    private IEnumerator FlickerRoutine(LightObject light)
    {
        while (true)
        {
            bool canFlicker = currentEnergyPercentage > 0f
                              && currentEnergyPercentage <= flickerThreshold
                              && (LightManager.Instance == null || (!LightManager.Instance.IsAlertActive && !LightManager.Instance.IsBlackout && !LightManager.Instance.IsRestoring));

            if (canFlicker && Random.value < flickerChance)
            {
                light.Toggle(false);
                yield return new WaitForSeconds(Random.Range(minFlickerTime, maxFlickerTime));
                if (LightManager.Instance == null || (!LightManager.Instance.IsAlertActive && !LightManager.Instance.IsBlackout))
                {
                    light.Toggle(true);
                }
            }
            yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
        }
    }
}