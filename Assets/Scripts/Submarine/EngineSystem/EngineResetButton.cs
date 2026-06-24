using UnityEngine;

public class EngineResetButton : MonoBehaviour, IInteractable
{
    [SerializeField] private EngineSystem engineSystem;

    public void Interact(PlayerCharacter player)
    {
        engineSystem.RestartEngine();
    }
}
