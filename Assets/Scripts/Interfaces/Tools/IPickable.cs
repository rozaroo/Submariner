using UnityEngine;

public interface IPickable
{
    GameObject GameObject { get; }
    Vector3 HoldPositionOffset { get; }
    Vector3 HoldRotationOffset { get; }
    void OnPickUp();
    void OnDrop();
}
