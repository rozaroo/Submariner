using UnityEngine;

public class TutorialSequenceManager : SequenceManager<TutorialContext>
{
    protected override void InitializeContext()
    {
        _context = new TutorialContext();
        _context.Setup();
    }
    
    #if UNITY_EDITOR

        [ContextMenu("AutoPopulate Blackboard (Find SequenceEntities)")]
        private void EditorAutoPopulate()
        {
            AutoPopulateBlackboard();
        }

        [ContextMenu("Clear Blackboard")]
        private void EditorClearBlackboard()
        {
            ClearBlackboard();
        }

    #endif
}