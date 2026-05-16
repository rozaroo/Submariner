using UnityEngine;

[CreateAssetMenu(menuName = "Map/Behaviours/Submarine Movement")]
public class Submarine2DMovementBehaviourSO : IconBehaviourSO
{
    public override void ApplyComponent(GameObject go)
    {
        go.AddComponent<Submarine2DMovementBehaviour>();
    }
}