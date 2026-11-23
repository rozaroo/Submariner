using System;
using Bitgem.VFX.StylisedWater;
using UnityEngine;

public class HullDamage : MonoBehaviour
{
    [Header("Repair Properties")]
    [SerializeField] private float repairDuration = 3f;

    [Header("Water Effect")]
    [SerializeField] private ParticleSystem waterLeak;
    [SerializeField] private Material bubbleMaterial;
    [SerializeField, Min(0f)] private float bubbleForwardOffset = 0.2f;

    private const float BubbleEmissionRate = 20f;
    private const float SubmersionMargin = 0.025f;
    private const int MaxBubbles = 256;

    public event Action<HullDamage> OnCrackRepaired;
    [SerializeField] private float _progress;

    private float _baseEmissionRate;
    private bool _showingBubbles;
    private ParticleSystem _bubbles;
    private ParticleSystem.Particle[] _bubbleParticles;

    public void Repair(float amount, Blowtorch activeTorch)
    {
        _progress += amount;
        UpdateLeakIntensity();

        if (_progress >= repairDuration)
        {
            if (activeTorch != null) activeTorch.StopWeldingEffects();
            Close();
        }
    }
    
    private void UpdateLeakIntensity()
    {
        if (waterLeak == null) return;
        float remaining = 1f - Mathf.Clamp01(_progress / repairDuration);
        var emission = waterLeak.emission;
        emission.rateOverTimeMultiplier = _baseEmissionRate * remaining;

        if (_bubbles == null) return;
        var bubbleEmission = _bubbles.emission;
        bubbleEmission.rateOverTime = BubbleEmissionRate * remaining;
    }

    private void LateUpdate()
    {
        WaterVolumeHelper water = WaterVolumeHelper.Instance;
        if (waterLeak == null || bubbleMaterial == null) return;

        Vector3 outlet = waterLeak.transform.position;
        float requiredDepth = _showingBubbles ? 0f : SubmersionMargin;
        bool submerged = water != null && water.isActiveAndEnabled
            && water.TryGetSurfaceHeight(outlet, out float surfaceHeight, matchRenderedSurface: true)
            && surfaceHeight - outlet.y > requiredDepth;

        if (_showingBubbles != submerged)
            SetBubbleMode(submerged);

        if (_showingBubbles) PopBubblesAtSurface(water);
    }

    private void SetBubbleMode(bool submerged)
    {
        _showingBubbles = submerged;
        if (submerged)
        {
            waterLeak.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (_bubbles == null) CreateBubbleEffect();
            UpdateLeakIntensity();
            _bubbles.Play();
        }
        else
        {
            _bubbles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            waterLeak.Play(true);
        }
    }

    private void CreateBubbleEffect()
    {
        GameObject emitter = new GameObject("Leak Bubbles");
        emitter.SetActive(false);
        emitter.layer = waterLeak.gameObject.layer;
        emitter.transform.SetParent(transform, false);
        Transform outlet = waterLeak.transform;
        Vector3 inward = Vector3.ProjectOnPlane(outlet.forward, Vector3.up).normalized;
        Vector3 bubbleOrigin = outlet.position + inward * bubbleForwardOffset;
        emitter.transform.SetPositionAndRotation(bubbleOrigin, Quaternion.Euler(-90f, 0f, 0f));
        _bubbles = emitter.AddComponent<ParticleSystem>();
        _bubbleParticles = new ParticleSystem.Particle[MaxBubbles];

        var main = _bubbles.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        main.maxParticles = MaxBubbles;
        main.startLifetime = 12f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.65f, 1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.085f);

        var shape = _bubbles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.radius = 0.035f;
        shape.angle = 7f;

        emitter.GetComponent<ParticleSystemRenderer>().sharedMaterial = bubbleMaterial;
        emitter.SetActive(true);
    }

    private void PopBubblesAtSurface(WaterVolumeHelper water)
    {
        int count = _bubbles.GetParticles(_bubbleParticles);
        bool popped = false;
        for (int i = 0; i < count; i++)
        {
            Vector3 position = _bubbleParticles[i].position;
            if (water.TryGetSurfaceHeight(position, out float surface, matchRenderedSurface: true)
                && position.y < surface)
                continue;

            _bubbleParticles[i].remainingLifetime = -1f;
            popped = true;
        }

        if (popped) _bubbles.SetParticles(_bubbleParticles, count);
    }

    private void Close()
    {
        OnCrackRepaired?.Invoke(this);
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        _progress = 0f;
        if (waterLeak != null)
        {
            if (_baseEmissionRate <= 0f)
                _baseEmissionRate = waterLeak.emission.rateOverTimeMultiplier;
            waterLeak.Clear(true);
            waterLeak.Play(true);
            UpdateLeakIntensity();
        }
        SFXManager.PostEvent("Start_HullBreachSFX", gameObject);
    }

    private void OnDisable()
    {
        _showingBubbles = false;
        if (waterLeak != null) waterLeak.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (_bubbles != null) _bubbles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        SFXManager.PostEvent("Stop_HullBreachSFX", gameObject);
    }
}
