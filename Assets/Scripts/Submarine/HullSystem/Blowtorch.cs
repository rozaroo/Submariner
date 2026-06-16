using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Blowtorch : MonoBehaviour, IInteractable, IPickable, IUsable
{
    [Header("Position Settings")]
    [SerializeField] private Vector3 holdOffset = new Vector3(-0.3f, -0.3f, 0.6f);

    [Header("Repair")]
    [SerializeField] private float repairRange = 2.5f;
    [SerializeField] private LayerMask crackLayer;
    
    [Header("Audio (Wwise)")]
    [SerializeField] private string blowtorchStartEvent = "Start_BlowtorchSFX";
    [SerializeField] private string blowtorchStopEvent = "Stop_BlowtorchSFX";
    
    private bool _isWelding;
    
    private Camera _camera;
    private Collider _collider;
    private Rigidbody _rb;
    
    public GameObject GameObject => gameObject;
    public Vector3 HoldPositionOffset => holdOffset;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _rb = GetComponent<Rigidbody>();
    }

    public void Interact(PlayerCharacter player)
    {
        player.InventorySystem.TryPickUp(this);
        _camera = player.CamController.MainCamera;
    }

    public void OnPickUp()
    {
        _rb.isKinematic = true;
        _collider.enabled = false;
    }
    
    public void OnDrop()
    {
        StopWeldingAudio();
        _rb.isKinematic = false;
        _collider.enabled = true;
    }
    
    public void UseItem()
    {
        //Maybe play animation/sound?
    }
    
    public void UseItemHold()
    {
        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);
        
        if (Physics.Raycast(ray, out RaycastHit hit, repairRange, crackLayer) && hit.collider.TryGetComponent(out HullDamage crack))
        {
            if (!_isWelding)
            {
                SFXManager.PostEvent(blowtorchStartEvent, gameObject);
                _isWelding = true;
            }
            
            crack.Repair(Time.deltaTime, this);
        }
        else
        {
            StopWeldingAudio();
        }
    }

    public void UseItemReleased()
    {
        StopWeldingAudio();
    }
    
    public void StopWeldingAudio()
    {
        if (_isWelding)
        {
            SFXManager.PostEvent(blowtorchStopEvent, gameObject);
            _isWelding = false;
        }
    }

}
