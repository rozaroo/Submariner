using UnityEngine;

public class EngineMiniGameComponent : MonoBehaviour, IInteractable
{
    [SerializeField] private EngineMiniGame engineMinigame;

    private Material tubeMaterial;

    [Header("Visual Feedback")]
    [SerializeField] private Renderer tubeRenderer;

    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color sequenceColor = Color.red;
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color hoverColor = Color.yellow;

    [SerializeField] private float normalEmission = 0f;
    [SerializeField] private float feedbackEmission = 1f;

    private bool _isShowingSequence;
    private bool _isCorrect;
    private bool _isHovered;

    private void Awake()
    {
        if (tubeRenderer != null)
        {
            tubeMaterial = tubeRenderer.material;
            ApplyVisual();
        }
    }

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
        _isShowingSequence = true;
        ApplyVisual();
    }

    public void ShowCorrectFeedback()
    {
        _isShowingSequence = false;
        _isCorrect = true;
        ApplyVisual();
    }

    public void TurnOffFeedback()
    {
        _isShowingSequence = false;
        _isCorrect = false;
        ApplyVisual();
    }

    public void SetHovered(bool isHovered)
    {
        if (_isHovered == isHovered) return;

        _isHovered = isHovered;
        ApplyVisual();
    }

    public void ClickInteract(PlayerCharacter player)
    {
        if (engineMinigame == null)
        {
            Debug.LogError($"[ENGINE MINIGAME] {name}: EngineMinigame no está asignado.");
            return;
        }

        Debug.Log($"[ENGINE MINIGAME] Component clicked: {gameObject.name}");

        engineMinigame.OnComponentInteracted(this);
    }

    private void ApplyVisual()
    {
        if (tubeMaterial == null) return;

        Color color = normalColor;
        float emission = normalEmission;

        if (_isCorrect)
        {
            color = correctColor;
            emission = feedbackEmission;
        }
        else if (_isShowingSequence)
        {
            color = sequenceColor;
            emission = feedbackEmission;
        }
        else if (_isHovered)
        {
            color = hoverColor;
            emission = feedbackEmission;
        }

        tubeMaterial.SetColor("_Color", color);
        tubeMaterial.SetFloat("_Emission", emission);
    }
}
