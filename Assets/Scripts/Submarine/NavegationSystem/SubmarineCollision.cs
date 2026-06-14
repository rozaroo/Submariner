using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class SubmarineCollision : MonoBehaviour
{
    [Header("Collision Settings")]
    [SerializeField] private float collisionCooldown = 1f;
    private float _lastCollisionTime = -100f;
    
    private void OnTriggerEnter(Collider other)
    {
        if (Time.time < _lastCollisionTime + collisionCooldown) return; //No need of Delta.Time/FixedTime

        if (other.gameObject.layer == LayerMask.NameToLayer("ExternalCollision"))
        {
            _lastCollisionTime = Time.time;
            Log.Info("Valid Collision");
            GameEventChannel<OnSubmarineCollision>.RaiseEvent(new OnSubmarineCollision(other));
        }
        else
        {
            Log.Info("NOT Valid Collision");
        }
    }
}
