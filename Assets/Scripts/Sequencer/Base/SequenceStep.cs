using System;
using UnityEngine;

public abstract class SequenceStep : ScriptableObject
{
    public bool waitUntilFinished = false;
    public abstract void EnterStep(SequenceContext context, Action onStepComplete);
    public virtual void ExitStep(SequenceContext context)
    { }
}
