using System.Collections;
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

    [SerializeField] private float normalEmission = 0f;
    [SerializeField] private float feedbackEmission = 1f;

    private void Awake()
    {
        if (tubeRenderer != null)
        {
            tubeMaterial = tubeRenderer.material;
            tubeMaterial.SetColor("_Color", normalColor);
            tubeMaterial.SetFloat("_Emission", normalEmission);
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
        if (tubeMaterial == null) return;

        tubeMaterial.SetColor("_Color", sequenceColor);
        tubeMaterial.SetFloat("_Emission", feedbackEmission);
    }

    public void ShowCorrectFeedback()
    {
        if (tubeMaterial == null) return;
        tubeMaterial.SetColor("_Color", correctColor);
        tubeMaterial.SetFloat("_Emission", feedbackEmission);
        StartCoroutine(ReturnToNormalAfterDelay());
    }
    public void TurnOffFeedback()
    {
        if (tubeMaterial == null) return;

        tubeMaterial.SetColor("_Color", normalColor);
        tubeMaterial.SetFloat("_Emission", normalEmission);
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
    private IEnumerator ReturnToNormalAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        TurnOffFeedback();
    }
}