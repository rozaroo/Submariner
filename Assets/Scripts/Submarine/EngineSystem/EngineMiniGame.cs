using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Emergency engine restart. The player first watches a sequence and then
/// repeats it by clicking the physical components at the engine station.
/// </summary>
public class EngineMiniGame : MonoBehaviour
{
    [Header("Engine")]
    [SerializeField] private EngineSystem engineSystem;

    [Header("Components")]
    [SerializeField] private List<EngineMiniGameComponent> components = new();

    [Header("Minigame Settings")]
    [SerializeField] private int totalRounds = 3;
    [SerializeField] private float timeLimit = 60f;
    [SerializeField] private float sequenceLightDuration = 0.5f;
    [SerializeField] private float sequenceGapDuration = 0.2f;
    [SerializeField] private float sequenceReplayDelay = 2f;
    [SerializeField] private float failedSequenceDelay = 0.5f;
    [SerializeField] private float roundTransitionInitialPause = 0.25f;
    [SerializeField] private float roundTransitionPulseDuration = 0.25f;
    [SerializeField] private int roundTransitionPulseCount = 2;
    [SerializeField] private float completionGreenHoldDuration = 0.35f;
    [SerializeField] private float completionFadeOutDelay = 0.2f;
    [SerializeField] private int errorsBeforeHullDamage = 5;
    [SerializeField] private int hullDamageOnErrorLimit = 2;
    [SerializeField] private HullDamageManager hullDamageManager;

    [Header("Timer 3D")]
    [SerializeField] private bool showTimer = true;
    [SerializeField] private bool createTimerIfMissing = true;
    [SerializeField] private float timerCharacterSize = 0.08f;
    [SerializeField] private Color timerColor = Color.white;
    [SerializeField] private TextMesh timerText;
    
    [Header("Audio (Wwise)")]
    [SerializeField] private string engineTubeFix = "Start_Tube_Repair_Engine";
    [SerializeField] private string engineSequenceFailed = "Start_Motor_Engine_Fail";
    [SerializeField] private string succeededRound = "Start_Engine_Minigame_SucceededStage";
    [SerializeField] private string onFailedMinigame = "Start_Engine_Minigame_Failed";

    private readonly List<int> _currentSequence = new();
    private int _currentRound;
    private int _currentInput;
    private int _totalErrors;
    private float _remainingTime;
    private bool _isActive;
    private bool _isCompleting;
    private bool _acceptingInput;
    private bool _hullDamageTriggered;
    private Coroutine _timerCoroutine;
    private Coroutine _sequenceCoroutine;
    private Coroutine _idleReplayCoroutine;
    private Coroutine _roundTransitionCoroutine;
    private Coroutine _completionCoroutine;

    public bool IsActive => _isActive;
    public bool IsAcceptingInput => _acceptingInput;
    public bool CanCancel => _isActive || _isCompleting;
    public bool CanStart => engineSystem != null && engineSystem.IsBroken() && !_isActive && !_isCompleting;
    public event System.Action Completed;

    private void Awake()
    {
        if (engineSystem == null) Debug.LogError("[ENGINE MINIGAME] EngineSystem no está asignado.");
        if (components.Count < 6)
            Debug.LogError($"[ENGINE MINIGAME] Se necesitan 6 componentes. Actualmente hay {components.Count}.");

        EnsureTimerLabel();
    }

    private void OnDisable()
    {
        StopRunningCoroutines();
    }

    private void EnsureTimerLabel()
    {
        if (!showTimer || timerText != null || !createTimerIfMissing) return;

        GameObject timerObject = new GameObject("EngineEmergencyTimer");
        timerObject.transform.SetParent(transform);
        timerObject.transform.localPosition = new Vector3(0.869f, -0.395f, 0f);
        timerObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        timerObject.layer = LayerMask.NameToLayer("Ignore Raycast");

        timerText = timerObject.AddComponent<TextMesh>();
        timerText.anchor = TextAnchor.MiddleCenter;
        timerText.alignment = TextAlignment.Center;
        timerText.characterSize = timerCharacterSize;
        timerText.color = timerColor;
        timerObject.SetActive(false);
    }

    public void StartMinigame()
    {
        if (_isActive) return;
        if (engineSystem == null || !engineSystem.IsBroken())
        {
            Debug.Log("[ENGINE MINIGAME] El reinicio de emergencia solo está disponible con el motor averiado.");
            return;
        }

        _isActive = true;
        _acceptingInput = false;
        _currentRound = 1;
        _currentInput = 0;
        _totalErrors = 0;
        _hullDamageTriggered = false;
        _remainingTime = timeLimit;
        TurnOffAllComponents();

        if (timerText != null) timerText.gameObject.SetActive(true);
        UpdateTimerUI();
        _timerCoroutine = StartCoroutine(EmergencyTimerRoutine());

        StartRoundWithNewSequence();
        Debug.Log("[ENGINE MINIGAME] REINICIO DE EMERGENCIA INICIADO");
    }

    public void CancelMinigame()
    {
        if (!CanCancel) return;

        _isActive = false;
        _isCompleting = false;
        _acceptingInput = false;
        StopRunningCoroutines();
        StopCompletionFeedback();
        TurnOffAllComponents();
        if (timerText != null) timerText.gameObject.SetActive(false);
        Debug.Log("[ENGINE MINIGAME] Reparación cancelada por el jugador.");
    }

    private IEnumerator EmergencyTimerRoutine()
    {
        while (_isActive && _remainingTime > 0f)
        {
            yield return new WaitForSeconds(1f);
            _remainingTime = Mathf.Max(0f, _remainingTime - 1f);
            UpdateTimerUI();
        }

        if (_isActive && _remainingTime <= 0f) TimeExpired();
    }

    private void StartRoundWithNewSequence()
    {
        _currentSequence.Clear();
        _currentInput = 0;

        int sequenceLength = Mathf.Clamp(_currentRound + 2, 1, components.Count);
        List<int> availableIndexes = new();
        for (int i = 0; i < components.Count; i++) availableIndexes.Add(i);

        for (int i = 0; i < sequenceLength; i++)
        {
            int availableIndex = Random.Range(0, availableIndexes.Count);
            _currentSequence.Add(availableIndexes[availableIndex]);
            availableIndexes.RemoveAt(availableIndex);
        }

        ShowCurrentSequence();
    }

    private void ShowCurrentSequence()
    {
        if (_sequenceCoroutine != null) StopCoroutine(_sequenceCoroutine);
        StopIdleSequenceReplay();
        _sequenceCoroutine = StartCoroutine(ShowSequence());
    }

    private IEnumerator ShowSequence()
    {
        _acceptingInput = false;
        TurnOffAllComponents();

        foreach (int index in _currentSequence)
        {
            EngineMiniGameComponent component = GetComponentAt(index);
            if (component == null) continue;

            component.ShowSequenceFeedback();
            yield return new WaitForSeconds(sequenceLightDuration);
            component.TurnOffFeedback();
            yield return new WaitForSeconds(sequenceGapDuration);
        }

        _sequenceCoroutine = null;
        _acceptingInput = _isActive;
        if (_acceptingInput) _idleReplayCoroutine = StartCoroutine(ReplaySequenceIfIdle());
        Debug.Log($"[ENGINE MINIGAME] Ronda {_currentRound}: secuencia lista para repetir.");
    }

    private IEnumerator ReplaySequenceIfIdle()
    {
        yield return new WaitForSeconds(sequenceReplayDelay);

        _idleReplayCoroutine = null;
        if (_isActive && _acceptingInput && _currentInput == 0)
        {
            ShowCurrentSequence();
        }
    }

    public void OnComponentInteracted(EngineMiniGameComponent component)
    {
        if (!_isActive || !_acceptingInput || component == null) return;

        // The player has begun the attempt, so do not interrupt it with another preview.
        StopIdleSequenceReplay();

        int componentIndex = components.IndexOf(component);
        if (componentIndex < 0 || _currentInput >= _currentSequence.Count) return;

        if (componentIndex != _currentSequence[_currentInput])
        {
            RegisterIncorrectInput();
            return;
        }

        SFXManager.PostEvent(engineTubeFix, gameObject);
        component.ShowCorrectFeedback();
        _currentInput++;

        if (_currentInput >= _currentSequence.Count) CompleteRound();
    }

    private void RegisterIncorrectInput()
    {
        _totalErrors++;
        _acceptingInput = false;
        SFXManager.PostEvent(engineSequenceFailed, gameObject);
        Debug.Log($"[ENGINE MINIGAME] Error {_totalErrors}. Se reinicia la ronda {_currentRound}.");
        ShowFailureFeedbackOnAllComponents();

        if (!_hullDamageTriggered && _totalErrors >= errorsBeforeHullDamage)
        {
            _hullDamageTriggered = true;
            SpawnHullDamage();
        }

        if (_sequenceCoroutine != null) StopCoroutine(_sequenceCoroutine);
        _sequenceCoroutine = StartCoroutine(RestartCurrentSequenceRoutine());
    }

    private IEnumerator RestartCurrentSequenceRoutine()
    {
        yield return new WaitForSeconds(failedSequenceDelay);
        _sequenceCoroutine = null;
        ShowCurrentSequence();
    }

    private void CompleteRound()
    {
        _acceptingInput = false;
        Debug.Log($"[ENGINE MINIGAME] Ronda {_currentRound} completada.");
        SFXManager.PostEvent(succeededRound, gameObject);
        if (_currentRound >= totalRounds)
        {
            CompleteMinigame();
            return;
        }

        _currentRound++;
        _roundTransitionCoroutine = StartCoroutine(AdvanceToNextRoundRoutine());
    }

    private IEnumerator AdvanceToNextRoundRoutine()
    {
        TurnOffAllComponents();
        yield return new WaitForSeconds(roundTransitionInitialPause);

        for (int i = 0; i < roundTransitionPulseCount; i++)
        {
            foreach (EngineMiniGameComponent component in components)
                if (component != null) component.ShowRoundTransitionFeedback();
            yield return new WaitForSeconds(roundTransitionPulseDuration);

            TurnOffAllComponents();
            yield return new WaitForSeconds(roundTransitionPulseDuration);
        }

        _roundTransitionCoroutine = null;
        if (_isActive) StartRoundWithNewSequence();
    }

    private void CompleteMinigame()
    {
        _isActive = false;
        _isCompleting = true;
        _acceptingInput = false;
        StopRunningCoroutines();
        if (timerText != null) timerText.gameObject.SetActive(false);
        _completionCoroutine = StartCoroutine(EmergencyRestartFeedback());
    }

    private IEnumerator EmergencyRestartFeedback()
    {
        // Three seconds of escalating feedback before the repaired engine is handed back to navigation.
        for (int i = 0; i < 3; i++)
        {
            foreach (EngineMiniGameComponent component in components)
                if (component != null) component.ShowCorrectFeedback();
            yield return new WaitForSeconds(0.5f);

            TurnOffAllComponents();
            yield return new WaitForSeconds(0.5f);
        }

        foreach (EngineMiniGameComponent component in components)
            if (component != null) component.ShowCompletionFeedback();

        yield return new WaitForSeconds(completionGreenHoldDuration);
        TurnOffAllComponents();
        yield return new WaitForSeconds(completionFadeOutDelay);

        if (engineSystem != null) engineSystem.RestartEngine();
        _isCompleting = false;
        _completionCoroutine = null;
        Debug.Log("[ENGINE MINIGAME] Motor reparado. Vuelve a la palanca de navegación.");
        Completed?.Invoke();
    }

    private void TimeExpired()
    {
        _isActive = false;
        _acceptingInput = false;
        StopRunningCoroutines();
        TurnOffAllComponents();
        if (timerText != null) timerText.gameObject.SetActive(false);

        Debug.Log("[ENGINE MINIGAME] Tiempo agotado: sobrecalentamiento crítico.");
        GameEventChannel<OnDeath>.RaiseEvent(new OnDeath(DeathType.EngineOverheat));
    }

    private void SpawnHullDamage()
    {
        if (hullDamageManager == null) hullDamageManager = FindFirstObjectByType<HullDamageManager>();
        if (hullDamageManager == null)
        {
            Debug.LogWarning("[ENGINE MINIGAME] No hay HullDamageManager para generar fugas.");
            return;
        }

        int spawnedDamage = hullDamageManager.SpawnImmediateDamage(hullDamageOnErrorLimit);
        Debug.Log($"[ENGINE MINIGAME] Daño estructural: {spawnedDamage} fugas generadas.");
    }

    private void TurnOffAllComponents()
    {
        foreach (EngineMiniGameComponent component in components)
            if (component != null) component.TurnOffFeedback();
    }

    private void ShowFailureFeedbackOnAllComponents()
    {
        foreach (EngineMiniGameComponent component in components)
            if (component != null) component.ShowFailureFeedback();
    }

    private EngineMiniGameComponent GetComponentAt(int index)
    {
        return index >= 0 && index < components.Count ? components[index] : null;
    }

    private void StopRunningCoroutines()
    {
        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }

        if (_sequenceCoroutine != null)
        {
            StopCoroutine(_sequenceCoroutine);
            _sequenceCoroutine = null;
        }

        StopIdleSequenceReplay();

        if (_roundTransitionCoroutine != null)
        {
            StopCoroutine(_roundTransitionCoroutine);
            _roundTransitionCoroutine = null;
        }
    }

    private void StopCompletionFeedback()
    {
        if (_completionCoroutine == null) return;

        StopCoroutine(_completionCoroutine);
        _completionCoroutine = null;
    }

    private void StopIdleSequenceReplay()
    {
        if (_idleReplayCoroutine == null) return;

        StopCoroutine(_idleReplayCoroutine);
        _idleReplayCoroutine = null;
    }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;
        timerText.text = $"ROUND {_currentRound}/{totalRounds}\nTIME: {Mathf.CeilToInt(_remainingTime)}";
    }
}
