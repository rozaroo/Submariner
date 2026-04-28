using System.Collections.Generic;
using UnityEngine;

public class EnergyPanelControl : MonoBehaviour
{
    [Header("References")] [SerializeField]
    private EnergySystem energySystem;

    [Header("Fuse Slots")] [SerializeField]
    private List<Fuse> fuseSlots = new();

    private int _burnedSlotIndex = -1;
    //TODO: Burned Fuse Effect
}
