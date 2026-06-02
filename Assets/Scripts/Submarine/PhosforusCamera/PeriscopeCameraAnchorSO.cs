using UnityEngine;

[CreateAssetMenu(menuName = "Anchors/Periscope Camera Anchor")]
public class PeriscopeCameraAnchorSO : ScriptableObject
{
    public PhosphorusCamera phosphorusCameraComponent { get; set; }
    public Camera _playerCamera { get; set; }
}
