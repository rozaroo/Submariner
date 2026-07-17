using UnityEngine;

public class FuseLabelTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        FuseLabelManager.Instance.ToggleLabels();
    }
}
