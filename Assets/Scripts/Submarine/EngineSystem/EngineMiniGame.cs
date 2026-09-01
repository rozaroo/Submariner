using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class EngineMiniGame : MonoBehaviour
{
    [Header("Engine")]
    [SerializeField] private EngineSystem engineSystem;

    [Header("Components")]
    [SerializeField] private List<EngineMiniGameComponent> components;

    [Header("Minigame Settings")]
    [SerializeField] private int totalRounds = 3;

    private List<int> _currentSequence = new List<int>();
    private int _currentRound;
    private int _currentInput;

    private bool _isActive;
    [Header("Emergency Timer")]
    [SerializeField] private float timeLimit = 120f;
    [Header("Timer 3D")]
    [SerializeField] private bool showTimer = true;
    [SerializeField] private bool createTimerIfMissing = true;
    [SerializeField] private float timerCharacterSize = 0.08f;
    [SerializeField] private Color timerColor = Color.white;
    private TextMesh timerText;

    private float _remainingTime;
    private Coroutine _timerCoroutine;

    private void Awake()
    {
        if (engineSystem == null) Debug.LogError("[ENGINE MINIGAME] EngineSystem no está asignado.");
        if (components == null || components.Count < 6)
        {
            Debug.LogError(
                $"[ENGINE MINIGAME] Se necesitan 6 componentes. " +
                $"Actualmente hay {components?.Count ?? 0}."
            );
        }
        EnsureTimerLabel();
    }
    private void EnsureTimerLabel()
    {
        if (!showTimer) return;
        if (timerText != null) return;
        if (!createTimerIfMissing) return;
        GameObject timerObject = new GameObject("EngineEmergencyTimer");
        timerObject.transform.SetParent(transform);
        timerText = timerObject.AddComponent<TextMesh>();
        timerText.anchor = TextAnchor.MiddleCenter;
        timerText.alignment = TextAlignment.Center;
        timerText.transform.localPosition = new Vector3(0.869f, -0.395f, 0f);
        timerText.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        timerText.characterSize = timerCharacterSize;
        timerText.color = timerColor;
        // No debe interferir con el sistema de interacción.
        timerObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        Collider[] colliders = timerObject.GetComponents<Collider>();
        foreach (Collider collider in colliders)
            collider.enabled = false;
        // Empieza oculto.
        timerObject.SetActive(false);
    }
    private void OnEnable()
    {
        GameEventChannel<OnEngineStateChanged>.OnEventRaised += OnEngineStateChanged;
    }
    private void OnDisable()
    {
        GameEventChannel<OnEngineStateChanged>.OnEventRaised -= OnEngineStateChanged;
    }
    private void OnEngineStateChanged(OnEngineStateChanged eventData)
    {
        if (eventData.State != EngineState.Broken)
            return;

        Debug.Log("[ENGINE MINIGAME] Engine is BROKEN.");
        StartMinigame();
    }
    public void StartMinigame()
    {
        if (_isActive)
        {
            Debug.Log("[ENGINE MINIGAME] El minijuego ya está activo.");
            return;
        }

        _isActive = true;
        _currentRound = 1;
        _currentInput = 0;
        foreach (EngineMiniGameComponent component in components)
        {
            if (component != null) component.TurnOffFeedback();
        }
        _remainingTime = timeLimit;
        if (timerText != null) timerText.gameObject.SetActive(true);
        UpdateTimerUI();
        if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
        _timerCoroutine = StartCoroutine(EmergencyTimerRoutine());

        Debug.Log($"[ENGINE MINIGAME] Timer started: {_remainingTime} seconds.");
        Debug.Log("[ENGINE MINIGAME] ==========================");
        Debug.Log("[ENGINE MINIGAME] REINICIO DE EMERGENCIA INICIADO");
        Debug.Log("[ENGINE MINIGAME] Ronda 1");

        GenerateSequence();
    }
    private IEnumerator EmergencyTimerRoutine()
    {
        while (_remainingTime > 0f && _isActive)
        {
            yield return new WaitForSeconds(1f);

            _remainingTime -= 1f;
            UpdateTimerUI();
            //Debug.Log(
            //    $"[ENGINE MINIGAME] Time remaining: {_remainingTime:F0}s"
            //);
        }
        if (_remainingTime <= 0f && _isActive) TimeExpired();
    }
    private void TimeExpired()
    {
        _isActive = false;
        if (timerText != null) timerText.gameObject.SetActive(false);
        Debug.Log("[ENGINE MINIGAME] ==========================");
        Debug.Log("[ENGINE MINIGAME] TIME EXPIRED.");
        Debug.Log("[ENGINE MINIGAME] EMERGENCY RESTART FAILED.");
        _timerCoroutine = null;
    }

    private void GenerateSequence()
    {
        _currentSequence.Clear();

        int sequenceLength = _currentRound + 2;

        for (int i = 0; i < sequenceLength; i++)
        {
            int randomIndex = Random.Range(0, components.Count);

            _currentSequence.Add(randomIndex);
        }

        Debug.Log(
            $"[ENGINE MINIGAME] Nueva secuencia: " +
            $"{string.Join(" -> ", _currentSequence)}"
        );
        StartCoroutine(ShowSequence());

        _currentInput = 0;
    }
    private IEnumerator ShowSequence()
    {
        Debug.Log("[ENGINE MINIGAME] Showing sequence...");

        foreach (int index in _currentSequence)
        {
            if (index < 0 || index >= components.Count) continue;

            EngineMiniGameComponent component = components[index];

            // Encender componente
            component.ShowSequenceFeedback();

            // Mantener la luz encendida durante 0.5 segundos
            yield return new WaitForSeconds(0.5f);

            // Apagar componente
            component.TurnOffFeedback();

            // Pequeña pausa antes del siguiente
            yield return new WaitForSeconds(0.2f);
        }

        Debug.Log("[ENGINE MINIGAME] Sequence finished.");
    }

    public void OnComponentInteracted(EngineMiniGameComponent component)
    {
        if (!_isActive)
        {
            Debug.Log("[ENGINE MINIGAME] Interacción ignorada. Minijuego inactivo.");
            return;
        }

        int componentIndex = components.IndexOf(component);

        if (componentIndex == -1)
        {
            Debug.LogWarning(
                $"[ENGINE MINIGAME] Componente no registrado: {component.name}"
            );

            return;
        }

        Debug.Log(
            $"[ENGINE MINIGAME] Componente presionado: " +
            $"{componentIndex}"
        );

        int expectedIndex = _currentSequence[_currentInput];

        if (componentIndex != expectedIndex)
        {
            Debug.Log(
                $"[ENGINE MINIGAME] INCORRECTO. " +
                $"Esperado: {expectedIndex} | " +
                $"Recibido: {componentIndex}"
            );
            //Poner efecto de respuesta incorrecta
            ResetCurrentRound();
            return;
        }

        Debug.Log(
            $"[ENGINE MINIGAME] Correcto: " +
            $"{componentIndex}");

        component.ShowCorrectFeedback();
        _currentInput++;
        if (_currentInput >= _currentSequence.Count) CompleteRound();
    }

    private void CompleteRound()
    {
        Debug.Log(
            $"[ENGINE MINIGAME] Ronda {_currentRound} completada."
        );

        if (_currentRound >= totalRounds)
        {
            CompleteMinigame();
            return;
        }

        _currentRound++;

        Debug.Log($"[ENGINE MINIGAME] Comenzando ronda {_currentRound}.");
        GenerateSequence();
    }

    private void ResetCurrentRound()
    {
        Debug.Log(
            $"[ENGINE MINIGAME] Ronda {_currentRound} fallida. Reiniciando."
        );

        _currentInput = 0;
    }

    private void CompleteMinigame()
    {
        _isActive = false;
        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }
        if (timerText != null) timerText.gameObject.SetActive(false);
        Debug.Log("[ENGINE MINIGAME] ==========================");
        Debug.Log("[ENGINE MINIGAME] REINICIO DE EMERGENCIA COMPLETADO");
        StartCoroutine(EmergencyRestartFeedback());
    }
    private IEnumerator EmergencyRestartFeedback()
    {
        Debug.Log("[ENGINE MINIGAME] Emergency restart feedback started.");

        for (int i = 0; i < 3; i++)
        {
            foreach (EngineMiniGameComponent component in components)
                if (component != null) component.ShowCorrectFeedback();
            yield return new WaitForSeconds(0.2f);

            foreach (EngineMiniGameComponent component in components)
                if (component != null) component.TurnOffFeedback();
            
            yield return new WaitForSeconds(0.2f);
        }

        Debug.Log("[ENGINE MINIGAME] Emergency restart feedback finished.");
        if (engineSystem != null) engineSystem.RestartEngine();
    }
    private void UpdateTimerUI()
    {
        if (timerText == null) return;
        int seconds = Mathf.CeilToInt(_remainingTime);
        timerText.text = $"TIME: {seconds}";
    }
}