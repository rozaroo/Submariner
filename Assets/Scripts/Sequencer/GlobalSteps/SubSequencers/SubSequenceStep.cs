using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Sequences/Groups/Sequential Group (Sub-Sequence)")]
public class SubSequenceStep : SequenceStep
{
    [Tooltip("Execution in ORDER.")]
    public List<SequenceStep> steps;
    
    private SequenceStep _currentActiveChild;

    public override void EnterStep(SequenceContext context, Action onStepComplete)
    {
        _currentActiveChild = null;

        if (steps == null || steps.Count == 0)
        {
            onStepComplete?.Invoke();
            return;
        }
        
        //Recursivity
        ExecuteNextChild(0, context, onStepComplete);
    }

    private void ExecuteNextChild(int index, SequenceContext context, Action onFinalComplete)
    {
        if (context.IsCancelled) return; 
    
        if (index >= steps.Count)
        {
            onFinalComplete?.Invoke();
            return;
        }
        
        _currentActiveChild = steps[index];

        if (_currentActiveChild.waitUntilFinished)
        {
            _currentActiveChild.EnterStep(context, () => 
            {
                if (_currentActiveChild != null)
                {
                    _currentActiveChild.ExitStep(context);
                    _currentActiveChild = null;
                }
                
                ExecuteNextChild(index + 1, context, onFinalComplete);
            });
        }
        else
        {
            _currentActiveChild.EnterStep(context, () => { });
            _currentActiveChild.ExitStep(context);
            _currentActiveChild = null;
            
            ExecuteNextChild(index + 1, context, onFinalComplete);
        }
    }

    public override void ExitStep(SequenceContext context)
    {
        if (_currentActiveChild != null)
        {
            _currentActiveChild.ExitStep(context);
            _currentActiveChild = null;
        }
    }
}