using UnityEngine;

public class JellyFishTest : MonoBehaviour//TODO: Eliminate Later.
{
    private void Start() 
    {
        if (TryGetComponent<Animator>(out var animator))
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            float randomOffset = Random.Range(0f, 1f);
            float randomSpeed = Random.Range(0.6f, 1.15f);
            animator.SetFloat("AnimationSpeed", randomSpeed);
            
            animator.Play(state.fullPathHash, 0, randomOffset);
        }
    }
}

