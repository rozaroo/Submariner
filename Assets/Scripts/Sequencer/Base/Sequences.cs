using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Sequences/BaseSequence")]
public class Sequences : ScriptableObject
{
    public List<SequenceStep> sequenceSteps;
}
