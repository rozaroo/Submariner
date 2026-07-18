using System.Collections;
using UnityEngine;

public class SubmarineLightManager : MonoBehaviour
{
    private Light[] submarineLights;

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
        submarineLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        submarineLights = System.Array.FindAll(submarineLights,light => light.gameObject.name == "Light");
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
            foreach (Light light in submarineLights)
                StartCoroutine(FlickerRoutine(light)); 
        }
    }

    private void OnEnergyChanged(OnEnergyPropertyChange data)
    {
        currentEnergyPercentage = data.currentEnergyPercentage;
        UpdateLightIntensity();
    }

    private void UpdateLightIntensity()
    {
        float t = currentEnergyPercentage / 100f;
        float intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
        foreach (Light light in submarineLights)
            if (light != null) light.intensity = intensity;
    }

    private IEnumerator FlickerRoutine(Light light)
    {
        while (true)
        {
            if (currentEnergyPercentage <= flickerThreshold && Random.value < flickerChance)
            {
                light.enabled = false;
                yield return new WaitForSeconds(Random.Range(minFlickerTime, maxFlickerTime));
                light.enabled = true;
            }
            yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
        }
    }
}