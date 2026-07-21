using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Sequences/Groups/Parallel Group")]
public class ParallelSequenceStep : SequenceStep
{
    [Tooltip("All the Steps at the SAME time.")]
    public List<SequenceStep> stepsToPlay;

    private List<SequenceStep> _activeSteps = new List<SequenceStep>();

    public override void EnterStep(SequenceContext context, Action onStepComplete)
    {
        _activeSteps.Clear();

        if (stepsToPlay == null || stepsToPlay.Count == 0)
        {
            onStepComplete?.Invoke();
            return;
        }

        int stepsCompleted = 0;
        int stepsToWaitFor = 0;
        
        foreach (var step in stepsToPlay)
        {
            if (step.waitUntilFinished) 
                stepsToWaitFor++;
        }
        
        if (stepsToWaitFor == 0)
        {
            foreach (var step in stepsToPlay) 
            {
                step.EnterStep(context, () => { });
                step.ExitStep(context);
            }
            onStepComplete?.Invoke();
            return;
        }
        
        Action onChildComplete = () =>
        {
            stepsCompleted++;
            if (stepsCompleted >= stepsToWaitFor)
            {
                onStepComplete?.Invoke();
            }
        };
        
        foreach (var step in stepsToPlay)
        {
            if (step.waitUntilFinished)
            {
                _activeSteps.Add(step); 
                step.EnterStep(context, onChildComplete);
            }
            else
            {
                step.EnterStep(context, () => { });
                step.ExitStep(context);
            }
        }
    }

    public override void ExitStep(SequenceContext context)
    {
        foreach (var step in _activeSteps)
        {
            step.ExitStep(context);
        }
        _activeSteps.Clear();
    }
}