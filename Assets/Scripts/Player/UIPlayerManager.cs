using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIPlayerManager : MonoBehaviour
{
    [Header("Interaction Prompt")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private RectTransform promptContainerRect;
    [SerializeField] private Image keyIcon;
    [SerializeField] private TMP_Text promptText;

    private void Start()
    {
        HidePrompt();
    }

    private void OnEnable()
    {
        GameEventChannel<OnInteraction>.OnEventRaised += OnInteractionDataReceived;
    }

    private void OnDisable()
    {
        GameEventChannel<OnInteraction>.OnEventRaised -= OnInteractionDataReceived;
    }

    private void OnInteractionDataReceived(OnInteraction obj)
    {
        if (string.IsNullOrEmpty(obj.InteractionFeedback.messageToDisplay))
        {
            HidePrompt();
            return;
        }

        ShowPrompt(obj.InteractionFeedback);
    }

    private void ShowPrompt(InteractionFeedback feedback)
    {
        if (interactionPrompt != null && !interactionPrompt.activeSelf) 
            interactionPrompt.SetActive(true);
        
        if (promptText != null) 
            promptText.text = feedback.messageToDisplay;
        
        if (keyIcon != null)
        {
            Sprite targetSprite = null;
            
            if (feedback.interactionUIData != null && feedback.interactionUIData.icon != null)
            {
                targetSprite = feedback.interactionUIData.icon;
            }
            
            bool hasIcon = targetSprite != null && feedback.canInteract;
            keyIcon.gameObject.SetActive(hasIcon);
            
            if (hasIcon)
            {
                keyIcon.sprite = targetSprite;
            }
        }
        
        if (promptContainerRect != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(promptContainerRect);
        }
    }

    public void HidePrompt()
    {
        if (interactionPrompt != null && interactionPrompt.activeSelf)
        {
            interactionPrompt.SetActive(false);
        }
    }
}