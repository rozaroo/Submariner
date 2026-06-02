using UnityEngine;
using Random = UnityEngine.Random;

public class JellyFishEvent : WorldMapElement, IEvent
{
    [Header("Patrol Properties")]
    [SerializeField] private float patrolSpeed = 3f;

    private FlockingCore _flockManager;
    private Transform _selfTransform;
    private Vector3 _patrolTarget;

    public bool IsActive { get; set; }

    private void Awake()
    {
        _selfTransform = transform;
    }

    private void Start()
    {
        _patrolTarget = _selfTransform.position + Random.insideUnitSphere * 20f;
        _patrolTarget.y = _selfTransform.position.y; 
    }

    private void Update()
    {
        if (UpdateMode == WorldUIUpdateMode.Dynamic && IsActive)
        {
            _selfTransform.position = Vector3.MoveTowards(_selfTransform.position, _patrolTarget, patrolSpeed * Time.deltaTime);
            
            if ((_patrolTarget - _selfTransform.position).sqrMagnitude < 1f)
            {
                _patrolTarget = _selfTransform.position + Random.insideUnitSphere * 30f;
                _patrolTarget.y = _selfTransform.position.y;
            }
        }
    }
    
    public void InjectFlockingEngine(FlockingCore core)
    {
        _flockManager = core;
    }
    
    public bool CheckConditions()
    {
        return true; 
    }

    public void Execute()
    {
        IsActive = true;
        Log.Info($"[{name}] - Submarine entered danger zone. Activating visual flock.");
        
        if (_flockManager != null) 
        {
            _flockManager.SetGroupVisibility(true);
            _flockManager.enabled = true;
        }
    }

    public void EndEvent()
    {
        IsActive = false;
        Log.Info($"[{name}] - Submarine escaped. Hiding flock and freezing calculations.");
        
        if (_flockManager != null) 
        {
            _flockManager.enabled = false;
            _flockManager.SetGroupVisibility(false);
        }
    }
}
