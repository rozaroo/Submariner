using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class MissionUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject missionPanel;
    [SerializeField] private TextMeshProUGUI objectiveText;
    
    [Header("Animation Settings")]
    [SerializeField] private Color defaultTextColor = Color.white;
    [SerializeField] private Color completedColor = Color.green;
    [SerializeField] private float showCompletedDuration = 0.8f;
    [SerializeField] private float fadeDuration = 0.5f;

    private CanvasGroup _canvasGroup;
    private Coroutine _transitionCoroutine;

    private void Awake()
    {
        if (missionPanel != null)
        {
            _canvasGroup = missionPanel.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = missionPanel.AddComponent<CanvasGroup>();
            }
        }
    }

    private void OnEnable()
    {
        GameEventChannel<OnMainEventAreaChanged>.OnEventRaised += UpdateMissionUI;
    }

    private void OnDisable()
    {
        GameEventChannel<OnMainEventAreaChanged>.OnEventRaised -= UpdateMissionUI;
    }
    
    private void UpdateMissionUI(OnMainEventAreaChanged eventData)
    {
        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
        }

        if (eventData.IsActive) 
        {
            _transitionCoroutine = StartCoroutine(TransitionToNewMission(eventData.ObjectiveText));
        }
        else
        {
            _transitionCoroutine = StartCoroutine(TransitionMissionComplete());
        }
    }

    private IEnumerator TransitionMissionComplete()
    {
        if (objectiveText != null)
        {
            objectiveText.color = completedColor;
        }
        
        yield return new WaitForSeconds(showCompletedDuration);
        
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            }
            yield return null;
        }
        
        if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        if (missionPanel != null) missionPanel.SetActive(false);
    }

    private IEnumerator TransitionToNewMission(string newObjective)
    {
        if (missionPanel != null) missionPanel.SetActive(true);
        if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        
        if (objectiveText != null) 
        {
            objectiveText.color = defaultTextColor;
            objectiveText.text = "Current Mission: " + newObjective;
        }
        
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            }
            yield return null;
        }
        
        if (_canvasGroup != null) _canvasGroup.alpha = 1f;
    }
}