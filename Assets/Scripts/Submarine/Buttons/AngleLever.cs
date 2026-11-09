
using UnityEngine;

public class AngleLever : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SubmarineMovement submarineMovement;

    [Header("Angle Settings")]
    [SerializeField] private float minAngle = -45f;
    [SerializeField] private float maxAngle = 45f;

    private float _currentAngle;

    public float CurrentAngle => _currentAngle;

    private void Start()
    {
        if (submarineMovement == null)
        {
            Debug.LogError("[AngleLever] SubmarineMovement no está asignado.");
            return;
        }

        _currentAngle = 0f;
    }

    public void SetAngle(float angle)
    {
        if (submarineMovement == null) return;
        submarineMovement.SetAngle(Mathf.Clamp(angle, minAngle, maxAngle));
        _currentAngle = submarineMovement.CurrentAngle;
    }

    public void ResetAngle()
    {
        SetAngle(0f);
    }
}
