using System.Collections.Generic;
using UnityEngine;

public class FuseLabelManager : MonoBehaviour
{
    public static FuseLabelManager Instance;
    [SerializeField] private Fuse[] fuses;
    private bool labelsVisible;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ToggleLabels()
    {
        labelsVisible = !labelsVisible;
        foreach (Fuse fuse in fuses)
        {
            if (fuse == null) continue;
            Transform label = fuse.transform.Find("FuseAmperageLabel");
            if (label != null) label.gameObject.SetActive(labelsVisible);
        }
    }
}
