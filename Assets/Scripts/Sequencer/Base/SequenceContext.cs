using System.Collections.Generic;
using UnityEngine;
using System;

[Flags]
public enum SequenceRestrictions
{
    None = 0,
    PlayerMovement = 1 << 0,
    PlayerLook = 1 << 1,
    UIInteractions = 1 << 2,
    All = ~0
}

public enum RestrictionAction { Add, Remove, Set }

public abstract class SequenceContext
{
    //Add Getters/Setters for required items in Inheritors
    private Dictionary<string, GameObject> _sceneBlackboard = new();
    
    private Dictionary<Type, object> _services = new();
    
    private HashSet<GameObject> _modifiedObjects = new();
    
    protected SequenceRestrictions _currentRestrictions = SequenceRestrictions.None;
    
    public bool IsCancelled { get; private set; }
    public event Action OnCancel;

    #region SceneObjectRegistry

    public void RegisterSceneObject(string id, GameObject obj)
    {
        _sceneBlackboard[id] = obj;
    }

    public GameObject GetSceneObject(string id)
    {
        if (_sceneBlackboard.TryGetValue(id, out var obj))
            return obj;
        
        Log.Error($"[Sequence Context] ID Object not Found.");
        return null;
    }

    #endregion

    #region Restrictions

    public void ModifyGlobalRestrictions(SequenceRestrictions flags, RestrictionAction action)
    {
        switch (action)
        {
            case RestrictionAction.Add:
                _currentRestrictions |= flags;
                break;
            case RestrictionAction.Remove:
                _currentRestrictions &= ~flags;
                break;
            case RestrictionAction.Set:
                _currentRestrictions = flags;
                break;
        }
        ApplyRestrictionsInternal(_currentRestrictions);
    }
    
    protected virtual void ApplyRestrictionsInternal(SequenceRestrictions activeFlags) { }

    #endregion
    
    public void CancelSequence()
    {
        if (IsCancelled) return;
        
        IsCancelled = true;
        
        OnCancel?.Invoke();
        
        OnCancel = null; //Avoids Leaks. Dont Remove.
    }
}
