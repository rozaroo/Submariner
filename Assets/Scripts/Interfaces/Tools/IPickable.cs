using UnityEngine;

public interface IPickable
{
    GameObject GameObject { get; }
    Vector3 HoldPositionOffset { get; }
    void OnPickUp();
    void OnDrop();
}
