using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class SubmarineCollision : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("ExternalCollision"))
        {
            Log.Info("Valid Collision");
            GameEventChannel<OnSubmarineCollision>.RaiseEvent(new OnSubmarineCollision(other));
        }
        else
        {
            Log.Info("NOT Valid Collision");
        }
    }
}
