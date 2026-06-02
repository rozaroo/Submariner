using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Fuse Recipe Catalog", menuName = "Energy/Fuse Recipe Catalog")]
public class FuseRecipeCatalogSO : ScriptableObject
{
    [Header("Part Amperages")]
    [SerializeField] private List<int> topPartAmperages = new() { 10, 15, 20 };
    [SerializeField] private List<int> corePartAmperages = new() { 5, 10, 15 };
    [SerializeField] private List<int> bottomPartAmperages = new() { 10, 15, 20 };

    public IReadOnlyList<int> TopPartAmperages => topPartAmperages;
    public IReadOnlyList<int> CorePartAmperages => corePartAmperages;
    public IReadOnlyList<int> BottomPartAmperages => bottomPartAmperages;

    public int GetRandomAchievableAmperage()
    {
        List<int> achievableAmperages = GetAchievableAmperages();
        if (achievableAmperages.Count == 0)
        {
            Log.Warning("[FuseRecipeCatalogSO] No achievable amperages configured");
            return 0;
        }

        int randomIndex = Random.Range(0, achievableAmperages.Count);
        return achievableAmperages[randomIndex];
    }

    public bool CanBuildAmperage(int amperage)
    {
        List<int> achievableAmperages = GetAchievableAmperages();
        return achievableAmperages.Contains(amperage);
    }

    public List<int> GetAchievableAmperages()
    {
        HashSet<int> uniqueAmperages = new();

        for (int topIndex = 0; topIndex < topPartAmperages.Count; topIndex++)
        {
            for (int coreIndex = 0; coreIndex < corePartAmperages.Count; coreIndex++)
            {
                for (int bottomIndex = 0; bottomIndex < bottomPartAmperages.Count; bottomIndex++)
                {
                    int totalAmperage = topPartAmperages[topIndex] + corePartAmperages[coreIndex] + bottomPartAmperages[bottomIndex];
                    uniqueAmperages.Add(totalAmperage);
                }
            }
        }

        List<int> achievableAmperages = new(uniqueAmperages);
        achievableAmperages.Sort();
        return achievableAmperages;
    }
}
