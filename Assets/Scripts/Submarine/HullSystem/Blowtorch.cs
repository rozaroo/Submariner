using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Blowtorch : MonoBehaviour, IInteractable, IPickable, IUsable
{
    [Header("Position Settings")]
    [SerializeField] private Vector3 holdPositionOffset = new Vector3(0.3f, -0.3f, 0.6f);
    [SerializeField] private Vector3 holdRotationOffset = new Vector3(0f, 0f, 0f);

    [Header("Repair")]
    [SerializeField] private float repairRange = 2.5f;
    [SerializeField] private LayerMask crackLayer;
    
    [Header("Audio (Wwise)")]
    [SerializeField] private string blowtorchStartEvent = "Start_BlowtorchSFX";
    [SerializeField] private string blowtorchStopEvent = "Stop_BlowtorchSFX";
    
    [Header("Visuals")]
    [SerializeField] private ParticleSystem flameParticles;
    
    [Header("Auto-Aim Procedural")]
    [SerializeField] private float aimSpeed = 8f;
    [SerializeField] private Vector3 aimRotationOffset = new Vector3(-5f, 0f, 0f);
    
    [Header("Interaction")]
    [SerializeField] private InteractionUIData interactionUIData;
    
    private bool _isWelding;
    private Quaternion _targetLocalRotation = Quaternion.identity;
    
    private Camera _camera;
    private Collider _collider;
    private Rigidbody _rb;
    private FuseWorkbench _activeWorkbench;
    
    public GameObject GameObject => gameObject;
    public Vector3 HoldPositionOffset => holdPositionOffset;
    public Vector3 HoldRotationOffset => holdRotationOffset;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _rb = GetComponent<Rigidbody>();
    }
    
    private void Update()
    {
        if (_rb.isKinematic)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, _targetLocalRotation, Time.deltaTime * aimSpeed);
        }
    }

    public void Interact(PlayerCharacter player)
    {
        player.InventorySystem.TryPickUp(this);
        _camera = player.CamController.MainCamera;
    }

    public InteractionFeedback SendInteractionFeedback(PlayerCharacter player)
    {
        if (player != null && player.InventorySystem != null)
        {
            if (player.InventorySystem.HeldItem != null)
            {
                return new InteractionFeedback("Holding Another Item", interactionUIData);
            }
        }
        return new InteractionFeedback("Pick up Blowtorch", interactionUIData);
    }

    public void OnPickUp()
    {
        _rb.isKinematic = true;
        _collider.enabled = false;
        _targetLocalRotation = Quaternion.Euler(holdRotationOffset);
    }
    
    public void OnDrop()
    {
        _activeWorkbench?.CancelHandheldSoldering();
        _activeWorkbench = null;
        StopWeldingEffects();
        _rb.isKinematic = false;
        _collider.enabled = true;
    }
    
    public void UseItem()
    {
        TrySolderWorkbench();
    }
    
    public void UseItemHold()
    {
        if (TrySolderWorkbench())
        {
            StopWeldingEffects();
            return;
        }

        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);
        
        if (Physics.Raycast(ray, out RaycastHit hit, repairRange, crackLayer) && hit.collider.TryGetComponent(out HullDamage crack))
        {
            if (!_isWelding)
            {
                SFXManager.PostEvent(blowtorchStartEvent, gameObject);
                if (flameParticles != null) flameParticles.Play();
                _isWelding = true;
            }
            
            crack.Repair(Time.deltaTime, this);
            
            Vector3 directionToCrack = hit.point - transform.position;
            
            Quaternion worldLookRotation = Quaternion.LookRotation(directionToCrack);
            
            if (transform.parent != null)
            {
                Quaternion baseLocalRotation = Quaternion.Inverse(transform.parent.rotation) * worldLookRotation;
                _targetLocalRotation = baseLocalRotation * Quaternion.Euler(aimRotationOffset);
            }
        }
        else
        {
            StopWeldingEffects();
            _targetLocalRotation = Quaternion.Euler(holdRotationOffset);
        }
    }

    public void UseItemReleased()
    {
        _activeWorkbench?.CancelHandheldSoldering();
        _activeWorkbench = null;
        StopWeldingEffects();
        _targetLocalRotation = Quaternion.Euler(holdRotationOffset);
    }

    private bool TrySolderWorkbench()
    {
        if (_camera == null)
        {
            return false;
        }

        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, repairRange))
        {
            return false;
        }

        FuseWorkbench workbench = hit.collider.GetComponentInParent<FuseWorkbench>();
        if (workbench == null)
        {
            return false;
        }

        _activeWorkbench = workbench;
        return workbench.TrySolderWithHandheldBlowtorch();
    }
    
    public void StopWeldingEffects()
    {
        if (_isWelding)
        {
            SFXManager.PostEvent(blowtorchStopEvent, gameObject);
            if (flameParticles != null) flameParticles.Stop();
            _isWelding = false;
        }
    }

}
