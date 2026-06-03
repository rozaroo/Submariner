using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyPanelControl : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private EnergySystem energySystem;

    [Header("Fuse Slots")]
    [SerializeField] private List<Fuse> fuseSlots = new();

    [Header("Fuse Amperage")]
    [SerializeField] private FuseRecipeCatalogSO fuseRecipeCatalog;
    [SerializeField] private List<int> fallbackRequiredAmperages = new() { 30, 35, 40, 45, 50 };
    [SerializeField] private List<int> requiredAmperages = new();
    [SerializeField] private float underAmperageFuseLifetime = 25f;
    [SerializeField] private float overAmperageBurnDelay = 1f;

    private readonly List<Transform> _slotParents = new();
    private readonly List<Vector3> _slotLocalPositions = new();
    private readonly List<Quaternion> _slotLocalRotations = new();
    private int _burnedSlotIndex = -1;
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
        CacheSlotTransforms();
        SetupRequiredAmperages();
        RegisterInitialFuses();
    }

    public void Interact(PlayerCharacter player)
    {
        if (energySystem == null)
        {
            Log.Warning("[EnergyPanelControl] Energy System Not Set");
            return;
        }

        if (!energySystem.IsFuseBroken)
        {
            Log.Info("Energy panel stable.");
            return;
        }

        if (TryInstallFuse(player))
        {
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
        if (!player.inventorySystem.TryPickUp(fuse))
        {
            InstallFuseInSlot(fuse, fuseIndex);
            return false;
        }

        fuseSlots[fuseIndex] = null;
        _burnedSlotIndex = fuseIndex;
        Log.Info("Burned fuse removed from energy panel.");
        return true;
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

            if (fuse != null)
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

    private void RegisterInitialFuses()
    {
        for (int i = 0; i < fuseSlots.Count; i++)
        {
            Fuse fuse = fuseSlots[i];
            if (fuse != null)
            {
                fuse.SetAmperage(requiredAmperages[i]);
                fuse.InstallInPanel(this);
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

        int randomIndex = Random.Range(0, functionalFuseIndexes.Count);
        _burnedSlotIndex = functionalFuseIndexes[randomIndex];
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
        if (_burnedSlotIndex < 0 || _burnedSlotIndex >= fuseSlots.Count)
        {
            return false;
        }

        if (fuseSlots[_burnedSlotIndex] != null)
        {
            return false;
        }

        if (!player.inventorySystem.TryGetHeldItem(out Fuse fuse))
        {
            return false;
        }

        if (!fuse.IsFunctional)
        {
            Log.Warning("The selected fuse is burned and cannot restore the panel.");
            player.inventorySystem.TryPickUp(fuse);
            return false;
        }

        int requiredAmperage = requiredAmperages[_burnedSlotIndex];
        InstallFuseInSlot(fuse, _burnedSlotIndex);

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

    private void InstallFuseInSlot(Fuse fuse, int slotIndex)
    {
        fuseSlots[slotIndex] = fuse;
        fuse.transform.SetParent(_slotParents[slotIndex]);
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

        if (energySystem != null)
        {
            energySystem.RestoreFuse();
        }

        Log.Info("Correct amperage fuse installed.");
    }

    private void RestoreEnergyWithUnderAmperageFuse(Fuse fuse)
    {
        StopWrongFuseCoroutine();

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
}
