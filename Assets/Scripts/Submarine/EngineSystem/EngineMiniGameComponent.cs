using UnityEngine;

public class EngineMiniGameComponent : MonoBehaviour, IInteractable
{
    [SerializeField] private EngineMiniGame engineMinigame;

    [Header("Visual Feedback")]
    [SerializeField] private Light indicatorLight;

    [SerializeField] private Color sequenceColor = Color.red;
    [SerializeField] private Color correctColor = Color.green;

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
    public void ShowSequenceFeedback()
    {
        if (indicatorLight == null)
        {
            Debug.LogWarning(
                $"[ENGINE MINIGAME] {name}: Indicator Light no está asignada."
            );
            return;
        }
        indicatorLight.gameObject.SetActive(true);
        indicatorLight.color = sequenceColor;
        indicatorLight.enabled = true;

        Debug.Log(
            $"[ENGINE MINIGAME] Showing sequence on: {gameObject.name}"
        );
    }

    public void ShowCorrectFeedback()
    {
        if (indicatorLight == null)
        {
            Debug.LogWarning(
                $"[ENGINE MINIGAME] {name}: Indicator Light no está asignada."
            );
            return;
        }

        indicatorLight.color = correctColor;
        indicatorLight.enabled = true;

        Debug.Log(
            $"[ENGINE MINIGAME] Correct component: {gameObject.name}"
        );
    }
    public void TurnOffFeedback()
    {
        if (indicatorLight == null) return;
        indicatorLight.enabled = false;
    }
}