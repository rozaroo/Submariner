using UnityEngine;

public class EngineMiniGameComponent : MonoBehaviour, IInteractable
{
    [SerializeField] private EngineMiniGame engineMinigame;

    public void Interact(PlayerCharacter player)
    {
        if (engineMinigame == null)
        {
            Debug.LogError($"[ENGINE MINIGAME] {name}: EngineMinigame no está asignado.");
            return;
        }

        Debug.Log($"[ENGINE MINIGAME] Component interacted: {gameObject.name}");

        engineMinigame.OnComponentInteracted(this);
    }
}