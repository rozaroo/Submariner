using System.Collections;
using UnityEngine;

public class SubmarineLightManager : MonoBehaviour
{
    public static SubmarineLightManager Instance { get; private set; }

    private LightObject[] submarineLights;

    [Header("Intensity")]
    [SerializeField] private float maxIntensity = 2f;
    [SerializeField] private float minIntensity = 0.2f;

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
        UpdateLightIntensity();

        if (enableFlicker)
        {
            foreach (LightObject light in submarineLights)
                StartCoroutine(FlickerRoutine(light));
        }
    }

    private void OnEnergyChanged(OnEnergyPropertyChange data)
    {
        currentEnergyPercentage = data.currentEnergyPercentage;
        UpdateLightIntensity();
    }

    public void UpdateLightIntensity()
    {
        float t = currentEnergyPercentage / 100f;
        float intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
        foreach (LightObject light in submarineLights)
            if (light != null) light.SetIntensity(intensity);
    }

    private IEnumerator FlickerRoutine(LightObject light)
    {
        while (true)
        {
            if (currentEnergyPercentage <= flickerThreshold && Random.value < flickerChance)
            {
                light.Toggle(false);
                yield return new WaitForSeconds(Random.Range(minFlickerTime, maxFlickerTime));
                light.Toggle(true);
            }
            yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
        }
    }
}