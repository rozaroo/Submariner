using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public abstract class SequenceManager<T> : MonoBehaviour where T : SequenceContext
{
    [Header("Desired Sequence")]
    public Sequences sequences;
    
    [Header("Context Local Scene")]
    public List<SceneObjectReference> sceneBlackboard;
    
    protected T _context; 
    private int _currentStepIndex = 0;

    private void Awake()
    {
        InitializeContext();
    }

    private void Start()
    {
        InitializeSequence();
    }
    
    protected abstract void InitializeContext();

    private void InitializeSequence()
    {
        foreach (var refItem in sceneBlackboard)
        {
            _context.RegisterSceneObject(refItem.Id, refItem.gameObject);
        }
        
        if (sequences != null && sequences.sequenceSteps.Count > 0)
        {
            ExecuteStep(_currentStepIndex);
        }
    }

    private void ExecuteStep(int index)
    {
        if (_context.IsCancelled) return;
        
        if (index >= sequences.sequenceSteps.Count)
        {
            Log.Info($"Finished Sequence on: {index}");
        
            return;
        }
        
        SequenceStep currentStep = sequences.sequenceSteps[index];
        Log.Info($"Executing Sequence: {currentStep}");
    
        if (currentStep.waitUntilFinished)
        {
            currentStep.EnterStep(_context, OnStepCompleted);
        }
        else
        {
            currentStep.EnterStep(_context, () => { });
            currentStep.ExitStep(_context);
            _currentStepIndex++;
            ExecuteStep(_currentStepIndex);
        }
    }

    private void OnStepCompleted()
    {
        if (_context.IsCancelled) return;
        
        sequences.sequenceSteps[_currentStepIndex].ExitStep(_context);
        _currentStepIndex++;
        ExecuteStep(_currentStepIndex);
    }

    public void SkipSequence() 
    {
        if (_context == null || _context.IsCancelled) return;

        Log.Info("[SequenceManager] Sequence Aborted.");
        
        if (sequences != null && _currentStepIndex < sequences.sequenceSteps.Count)
        {
            sequences.sequenceSteps[_currentStepIndex].ExitStep(_context);
        }
    
        _context.CancelSequence();
        _context.ModifyGlobalRestrictions(SequenceRestrictions.All, RestrictionAction.Remove);
    }
    
    #region PopulationTools

    #if UNITY_EDITOR
    protected void AutoPopulateBlackboard()
    {
        sceneBlackboard.Clear();
        SequenceEntity[] entities = FindObjectsByType<SequenceEntity>(FindObjectsSortMode.None);

        foreach (var entity in entities)
        {
            if (!string.IsNullOrEmpty(entity.Id))
            {
                sceneBlackboard.Add(new SceneObjectReference 
                { 
                    Id = entity.Id, 
                    gameObject = entity.gameObject 
                });
            }
        }
        OnValidate();
        EditorUtility.SetDirty(this);
        Log.Info($"[SequenceManager] Added {entities.Length} objects automatically.");
    }
    
    protected void ClearBlackboard()
    {
        sceneBlackboard.Clear();
        EditorUtility.SetDirty(this);
        Log.Info("[SequenceManager] Removed All Entities.");
    }
    
    
    protected void OnValidate()
    {
        if (sceneBlackboard == null) return;

        HashSet<string> idChecker = new HashSet<string>();

        foreach (var item in sceneBlackboard)
        {
            if (string.IsNullOrEmpty(item.Id)) continue;
            
            if (!idChecker.Add(item.Id))
            {
                Log.Info($"[SequenceManager] Duplicate on Blackboard: '{item.Id}'. Overwriting first added.");
            }
        }
    }
    
    #endif

    #endregion
}