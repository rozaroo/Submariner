using UnityEngine;

[CreateAssetMenu(menuName = "Anchors/Periscope Camera Anchor")]
public class PeriscopeCameraAnchorSO : ScriptableObject
{
    public PhosphorusCamera phosphorusCameraComponent { get; set; }
    public PeriscopeFlash3D flashComponent { get; set; }
    public Camera playerCamera { get; set; }
}
