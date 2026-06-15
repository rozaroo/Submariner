using UnityEngine;
using TMPro;

public class DeathUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI deathReasonText;

    private void OnEnable()
    {
      
        GameEventChannel<OnDeath>.OnEventRaised += OnPlayerDeath;
    }

    private void OnDisable()
    {
        
        GameEventChannel<OnDeath>.OnEventRaised -= OnPlayerDeath;
    }

    private void OnPlayerDeath(OnDeath ev)
    {
        
        deathReasonText.gameObject.SetActive(true);

        
        switch (ev.TypeOfDeath)
        {
            case DeathType.SubmarineSunk:
                deathReasonText.text = "The submarine has sunk.";
                break;

            case DeathType.OxygenDepravation:
                deathReasonText.text = "You ran out of oxygen.";
                break;

            case DeathType.SkillIssue:
                deathReasonText.text = "Skill issue.";
                break;

            default:
                deathReasonText.text = "You lost.";
                break;
        }
    }
}