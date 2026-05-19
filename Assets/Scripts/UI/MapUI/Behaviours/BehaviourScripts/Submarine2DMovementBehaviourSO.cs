using UnityEngine;

[CreateAssetMenu(menuName = "Map/Behaviours/Submarine Movement")]
public class Submarine2DMovementBehaviourSO : IconBehaviourSO
{
    [Header("Properties")]
    [SerializeField] private float smoothTime = 0.3f;
    [SerializeField] private float rotationSmoothTime = 0.15f;
    [SerializeField] private float maxMovementSpeed = 10f;
    [SerializeField] private float maxRotationSpeed = 10f;
    [SerializeField] private float offsetRotation = -90f;
    [SerializeField] private float distanceOffset = 0.1f;
    public override void ApplyComponent(GameObject go)
    {
        var behaviour = go.AddComponent<Submarine2DMovementBehaviour>();
        behaviour.Setup(smoothTime, rotationSmoothTime, maxMovementSpeed, maxRotationSpeed, offsetRotation, distanceOffset);
    }
}