using System.Collections.Generic;
using UnityEngine;

public class EnergyToolbox : MonoBehaviour, IInteractable
{
    [Header("Fuse Puzzle")]
    [SerializeField] private Fuse functionalFusePrefab;
    [SerializeField] private List<GameObject> uselessToolPrefabs = new();
    [SerializeField] private List<Transform> spawnPoints = new();
    [SerializeField] private Transform itemsParent;
    [SerializeField] private bool generateOnlyOnce = true;

    private bool _hasGeneratedItems;

    public void Interact(PlayerCharacter player)
    {
        if (generateOnlyOnce && _hasGeneratedItems)
        {
            Log.Info("Energy toolbox already searched.");
            return;
        }

        GenerateItems();
    }

    private void GenerateItems()
    {
        if (functionalFusePrefab == null)
        {
            Log.Warning("[EnergyToolbox] Functional Fuse Prefab Not Set");
            return;
        }

        if (spawnPoints.Count == 0)
        {
            Log.Warning("[EnergyToolbox] Spawn Points Not Set");
            return;
        }

        Transform parent = transform;
        if (itemsParent != null)
        {
            parent = itemsParent;
        }

        int functionalFuseIndex = Random.Range(0, spawnPoints.Count);

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            Transform spawnPoint = spawnPoints[i];
            if (spawnPoint == null)
            {
                continue;
            }

            if (i == functionalFuseIndex)
            {
                Fuse fuse = Instantiate(functionalFusePrefab, spawnPoint.position, spawnPoint.rotation, parent);
                fuse.Restore();
                continue;
            }

            SpawnUselessTool(spawnPoint, parent);
        }

        _hasGeneratedItems = true;
        Log.Info("Energy toolbox generated a hidden functional fuse.");
    }

    private void SpawnUselessTool(Transform spawnPoint, Transform parent)
    {
        if (uselessToolPrefabs.Count == 0)
        {
            return;
        }

        int randomIndex = Random.Range(0, uselessToolPrefabs.Count);
        GameObject uselessToolPrefab = uselessToolPrefabs[randomIndex];
        if (uselessToolPrefab == null)
        {
            return;
        }

        Instantiate(uselessToolPrefab, spawnPoint.position, spawnPoint.rotation, parent);
    }
}
