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
    private bool _isHeld;
    private Quaternion _targetLocalRotation = Quaternion.identity;
    
    private Camera _camera;
    private Collider _collider;
    private Rigidbody _rb;
    private FuseWorkbench _activeWorkbench;
    private Vector3 _solderTargetLocalPoint;
    
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
        if (_isHeld)
        {
            if (_activeWorkbench != null && flameParticles != null)
            {
                Transform emitter = flameParticles.transform;
                Vector3 target = _activeWorkbench.transform.TransformPoint(_solderTargetLocalPoint);
                // Aim from the nozzle, which is offset from the tool's pivot.
                Quaternion correction = Quaternion.FromToRotation(emitter.forward, target - emitter.position);
                Quaternion worldRotation = correction * transform.rotation;
                _targetLocalRotation = Quaternion.Inverse(transform.parent.rotation) * worldRotation;
            }

            transform.localRotation = Quaternion.Slerp(transform.localRotation, _targetLocalRotation, Time.deltaTime * aimSpeed);
        }
    }

    private void OnDisable()
    {
        _activeWorkbench?.CancelHandheldSoldering();
    }

    public void Interact(PlayerCharacter player)
    {
        player.InventorySystem.TryPickUp(this);
        _camera = player.CamController.MainCamera;
    }

    public InteractionFeedback SendInteractionFeedback(PlayerCharacter player)
    {
        if (!_collider.enabled)
        {
            return new InteractionFeedback("", interactionUIData, false);
        }

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
        _isHeld = true;
        _rb.isKinematic = true;
        _collider.enabled = false;
        _targetLocalRotation = Quaternion.Euler(holdRotationOffset);
    }
    
    public void OnDrop()
    {
        _isHeld = false;
        UseItemReleased();
        _rb.isKinematic = false;
        _collider.enabled = true;
    }

    public void Dock()
    {
        _isHeld = false;
        UseItemReleased();
        if (!_rb.isKinematic)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
        _rb.isKinematic = true;
        _collider.enabled = false;
    }
    
    public void UseItem()
    {
        TrySolderWorkbench();
    }
    
    public void UseItemHold()
    {
        if (TrySolderWorkbench())
        {
            return;
        }

        _activeWorkbench?.CancelHandheldSoldering();
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

        return workbench.TrySolderWithHandheldBlowtorch(this);
    }

    public void BeginFuseSoldering(FuseWorkbench workbench, Vector3 jointPosition)
    {
        _activeWorkbench?.CancelHandheldSoldering();
        StopWeldingEffects();
        _activeWorkbench = workbench;
        _solderTargetLocalPoint = workbench.transform.InverseTransformPoint(jointPosition);
        if (flameParticles != null) flameParticles.Play();
    }

    public void EndFuseSoldering()
    {
        _activeWorkbench = null;
        _targetLocalRotation = Quaternion.Euler(holdRotationOffset);
        if (flameParticles != null) flameParticles.Stop();
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
