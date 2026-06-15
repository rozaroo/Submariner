using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyPanelControl : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private EnergySystem energySystem;

    [Header("Fuse Slots")]
    [SerializeField] private Fuse fusePrefab;
    [SerializeField] private bool generateInitialFusesOnStart = true;
    [SerializeField] private int generatedFuseSlotCount;
    [SerializeField] private List<Fuse> fuseSlots = new();
    [SerializeField] private List<Transform> fuseSlotPivots = new();

    [Header("Fuse Amperage")]
    [SerializeField] private FuseRecipeCatalogSO fuseRecipeCatalog;
    [SerializeField] private List<int> fallbackRequiredAmperages = new() { 30, 35, 40, 45, 50 };
    [SerializeField] private List<int> requiredAmperages = new();
    [SerializeField] private float underAmperageThresholdMultiplier = 0.5f;
    [SerializeField] private float underAmperageFuseLifetime = 25f;
    [SerializeField] private float overAmperageBurnDelay = 1f;

    private readonly List<Transform> _slotParents = new();
    private readonly List<Vector3> _slotLocalPositions = new();
    private readonly List<Quaternion> _slotLocalRotations = new();
    private int _burnedSlotIndex = -1;
    private int _lastInstalledFuseSlotIndex = -1;
    private Coroutine _wrongFuseCoroutine;

    private void OnEnable()
    {
        if (energySystem != null)
        {
            energySystem.FuseBurned += OnFuseBurned;
            energySystem.FuseRestored += OnFuseRestored;
        }
    }

    private void OnDisable()
    {
        if (energySystem != null)
        {
            energySystem.FuseBurned -= OnFuseBurned;
            energySystem.FuseRestored -= OnFuseRestored;
        }
    }

    private void Start()
    {
        EnsureSlotListSize();
        CacheSlotTransforms();
        SetupRequiredAmperages();
        GenerateInitialFuses();
        RegisterInitialFuses();
    }

    public void Interact(PlayerCharacter player)
    {
        if (energySystem == null)
        {
            Log.Warning("[EnergyPanelControl] Energy System Not Set");
            return;
        }

        if (TryInstallFuse(player))
        {
            return;
        }

        if (!energySystem.IsFuseBroken)
        {
            Log.Info("Energy panel stable.");
            return;
        }

        if (TryRemoveBurnedFuse(player))
        {
            return;
        }

        Log.Info("Energy panel needs a functional fuse.");
    }

    public bool TryRemoveFuse(Fuse fuse, PlayerCharacter player)
    {
        if (fuse == null || player == null)
        {
            return false;
        }

        int fuseIndex = fuseSlots.IndexOf(fuse);
        if (fuseIndex < 0 || !fuse.IsBurned)
        {
            return false;
        }

        fuse.DetachFromPanel();
        if (!player.InventorySystem.TryPickUp(fuse))
        {
            InstallFuseInSlot(fuse, fuseIndex);
            return false;
        }

        fuseSlots[fuseIndex] = null;
        _burnedSlotIndex = fuseIndex;
        Log.Info("Burned fuse removed from energy panel.");
        return true;
    }

    private void EnsureSlotListSize()
    {
        int slotCount = GetConfiguredSlotCount();
        if (slotCount == 0)
        {
            Log.Warning("[EnergyPanelControl] No fuse slots or pivots configured");
            return;
        }

        while (fuseSlots.Count < slotCount)
        {
            fuseSlots.Add(null);
        }
    }

    private int GetConfiguredSlotCount()
    {
        int slotCount = Mathf.Max(fuseSlots.Count, fuseSlotPivots.Count);
        slotCount = Mathf.Max(slotCount, generatedFuseSlotCount);
        return slotCount;
    }

    private void CacheSlotTransforms()
    {
        _slotParents.Clear();
        _slotLocalPositions.Clear();
        _slotLocalRotations.Clear();

        for (int i = 0; i < fuseSlots.Count; i++)
        {
            Fuse fuse = fuseSlots[i];
            Transform slotParent = transform;
            Vector3 slotLocalPosition = Vector3.zero;
            Quaternion slotLocalRotation = Quaternion.identity;

            if (TryGetSlotPivot(i, out Transform slotPivot))
            {
                slotParent = slotPivot;
            }
            else if (fuse != null)
            {
                slotParent = fuse.transform.parent;
                slotLocalPosition = fuse.transform.localPosition;
                slotLocalRotation = fuse.transform.localRotation;
            }

            _slotParents.Add(slotParent);
            _slotLocalPositions.Add(slotLocalPosition);
            _slotLocalRotations.Add(slotLocalRotation);
        }
    }

    private void GenerateInitialFuses()
    {
        if (!generateInitialFusesOnStart)
        {
            return;
        }

        if (fusePrefab == null)
        {
            Log.Warning("[EnergyPanelControl] Fuse Prefab Not Set");
            return;
        }

        for (int i = 0; i < fuseSlots.Count; i++)
        {
            if (fuseSlots[i] != null)
            {
                continue;
            }

            Fuse generatedFuse = Instantiate(fusePrefab);
            generatedFuse.Restore();
            fuseSlots[i] = generatedFuse;
        }
    }

    private void RegisterInitialFuses()
    {
        for (int i = 0; i < fuseSlots.Count; i++)
        {
            Fuse fuse = fuseSlots[i];
            if (fuse != null)
            {
                fuse.SetAmperage(requiredAmperages[i]);
                InstallFuseInSlot(fuse, i);
            }
        }
    }

    private void OnFuseBurned()
    {
        List<int> functionalFuseIndexes = new();

        for (int i = 0; i < fuseSlots.Count; i++)
        {
            Fuse fuse = fuseSlots[i];
            if (fuse != null && fuse.IsFunctional)
            {
                functionalFuseIndexes.Add(i);
            }
        }

        if (functionalFuseIndexes.Count == 0)
        {
            Log.Warning("[EnergyPanelControl] No functional fuses available to burn");
            return;
        }

        if (TryGetLastInstalledFunctionalFuseSlotIndex(out int lastInstalledFuseSlotIndex))
        {
            _burnedSlotIndex = lastInstalledFuseSlotIndex;
        }
        else
        {
            int randomIndex = Random.Range(0, functionalFuseIndexes.Count);
            _burnedSlotIndex = functionalFuseIndexes[randomIndex];
        }

        fuseSlots[_burnedSlotIndex].Burn();
        Log.Info($"Fuse burned in slot {_burnedSlotIndex}");
    }

    private void OnFuseRestored()
    {
        _burnedSlotIndex = -1;
    }

    private bool TryRemoveBurnedFuse(PlayerCharacter player)
    {
        if (_burnedSlotIndex < 0 || _burnedSlotIndex >= fuseSlots.Count)
        {
            return false;
        }

        Fuse burnedFuse = fuseSlots[_burnedSlotIndex];
        return TryRemoveFuse(burnedFuse, player);
    }

    private bool TryInstallFuse(PlayerCharacter player)
    {
        if (!TryGetRepairSlotIndex(out int repairSlotIndex))
        {
            if (energySystem != null && energySystem.IsFuseBroken)
            {
                Log.Info("Remove the burned fuse before installing a new one.");
            }

            return false;
        }

        if (fuseSlots[repairSlotIndex] != null)
        {
            Log.Info("Remove the burned fuse before installing a new one.");
            return false;
        }

        if (!player.InventorySystem.TryExtractHeldItem(out Fuse fuse))
        {
            Log.Info("Hold a functional fuse to install it.");
            return false;
        }

        if (!fuse.IsFunctional)
        {
            Log.Warning("The selected fuse is burned and cannot restore the panel.");
            player.InventorySystem.TryPickUp(fuse);
            return false;
        }

        _burnedSlotIndex = repairSlotIndex;
        int requiredAmperage = GetRequiredAmperage(repairSlotIndex);
        InstallFuseInSlot(fuse, repairSlotIndex);
        _lastInstalledFuseSlotIndex = repairSlotIndex;

        if (fuse.Amperage == requiredAmperage)
        {
            RestoreEnergyWithCorrectFuse();
        }
        else if (fuse.Amperage < requiredAmperage)
        {
            RestoreEnergyWithUnderAmperageFuse(fuse);
        }
        else
        {
            RestoreEnergyWithOverAmperageFuse(fuse);
        }

        return true;
    }

    private bool TryGetRepairSlotIndex(out int repairSlotIndex)
    {
        repairSlotIndex = -1;

        if (_burnedSlotIndex >= 0 && _burnedSlotIndex < fuseSlots.Count)
        {
            if (fuseSlots[_burnedSlotIndex] == null)
            {
                repairSlotIndex = _burnedSlotIndex;
                return true;
            }

            return false;
        }

        for (int i = 0; i < fuseSlots.Count; i++)
        {
            if (fuseSlots[i] == null)
            {
                repairSlotIndex = i;
                return true;
            }
        }

        return false;
    }

    private bool TryGetLastInstalledFunctionalFuseSlotIndex(out int slotIndex)
    {
        slotIndex = -1;

        if (_lastInstalledFuseSlotIndex < 0 || _lastInstalledFuseSlotIndex >= fuseSlots.Count)
        {
            return false;
        }

        Fuse fuse = fuseSlots[_lastInstalledFuseSlotIndex];
        if (fuse == null || !fuse.IsFunctional)
        {
            return false;
        }

        slotIndex = _lastInstalledFuseSlotIndex;
        return true;
    }

    private void InstallFuseInSlot(Fuse fuse, int slotIndex)
    {
        fuseSlots[slotIndex] = fuse;
        fuse.transform.SetParent(_slotParents[slotIndex], false);
        fuse.transform.localPosition = _slotLocalPositions[slotIndex];
        fuse.transform.localRotation = _slotLocalRotations[slotIndex];
        fuse.InstallInPanel(this);
        Log.Info("Fuse installed in energy panel.");
    }

    private void SetupRequiredAmperages()
    {
        requiredAmperages.Clear();

        for (int i = 0; i < fuseSlots.Count; i++)
        {
            requiredAmperages.Add(GetRandomRequiredAmperage());
        }
    }

    public int GetRequiredAmperage(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= requiredAmperages.Count)
        {
            return 0;
        }

        return requiredAmperages[slotIndex];
    }

    public int GetBurnedSlotRequiredAmperage()
    {
        return GetRequiredAmperage(_burnedSlotIndex);
    }

    private bool TryGetSlotPivot(int slotIndex, out Transform slotPivot)
    {
        slotPivot = null;

        if (slotIndex < 0 || slotIndex >= fuseSlotPivots.Count)
        {
            return false;
        }

        slotPivot = fuseSlotPivots[slotIndex];
        return slotPivot != null;
    }

    private int GetRandomRequiredAmperage()
    {
        if (fuseRecipeCatalog != null)
        {
            int amperage = fuseRecipeCatalog.GetRandomAchievableAmperage();
            if (amperage > 0)
            {
                return amperage;
            }
        }

        if (fallbackRequiredAmperages.Count == 0)
        {
            Log.Warning("[EnergyPanelControl] No fallback required amperages configured");
            return 40;
        }

        int randomIndex = Random.Range(0, fallbackRequiredAmperages.Count);
        return fallbackRequiredAmperages[randomIndex];
    }

    private void RestoreEnergyWithCorrectFuse()
    {
        StopWrongFuseCoroutine();
        ResetEnergyThreshold();

        if (energySystem != null)
        {
            energySystem.RestoreFuse();
        }

        Log.Info("Correct amperage fuse installed.");
    }

    private void RestoreEnergyWithUnderAmperageFuse(Fuse fuse)
    {
        StopWrongFuseCoroutine();
        ApplyUnderAmperageEnergyThreshold();

        if (energySystem != null)
        {
            energySystem.RestoreFuse();
        }

        _wrongFuseCoroutine = StartCoroutine(BurnInstalledFuseAfterDelay(fuse, underAmperageFuseLifetime));
        Log.Warning("Under amperage fuse installed. It will not last long.");
    }

    private void RestoreEnergyWithOverAmperageFuse(Fuse fuse)
    {
        StopWrongFuseCoroutine();
        ResetEnergyThreshold();

        if (energySystem != null)
        {
            energySystem.RestoreFuse();
        }

        _wrongFuseCoroutine = StartCoroutine(BurnInstalledFuseAfterDelay(fuse, overAmperageBurnDelay));
        Log.Warning("Over amperage fuse installed. It is burning immediately.");
    }

    private IEnumerator BurnInstalledFuseAfterDelay(Fuse fuse, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (fuse == null)
        {
            _wrongFuseCoroutine = null;
            yield break;
        }

        int fuseIndex = fuseSlots.IndexOf(fuse);
        if (fuseIndex < 0)
        {
            _wrongFuseCoroutine = null;
            yield break;
        }

        fuse.Burn();
        _burnedSlotIndex = fuseIndex;

        if (energySystem != null)
        {
            energySystem.BreakFuseFromPanel();
        }

        Log.Warning("Wrong amperage fuse burned.");
        _wrongFuseCoroutine = null;
    }

    private void StopWrongFuseCoroutine()
    {
        if (_wrongFuseCoroutine != null)
        {
            StopCoroutine(_wrongFuseCoroutine);
            _wrongFuseCoroutine = null;
        }
    }

    private void ResetEnergyThreshold()
    {
        if (energySystem == null)
        {
            return;
        }

        energySystem.ResetFuseBreakConsumptionThreshold();
    }

    private void ApplyUnderAmperageEnergyThreshold()
    {
        if (energySystem == null)
        {
            return;
        }

        energySystem.ApplyFuseBreakConsumptionThresholdMultiplier(underAmperageThresholdMultiplier);
    }
}
