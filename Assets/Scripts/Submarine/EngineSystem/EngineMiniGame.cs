using System.Collections.Generic;
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

    private void Awake()
    {
        if (engineSystem == null)
        {
            Debug.LogError("[ENGINE MINIGAME] EngineSystem no está asignado.");
        }

        if (components == null || components.Count < 6)
        {
            Debug.LogError(
                $"[ENGINE MINIGAME] Se necesitan 6 componentes. " +
                $"Actualmente hay {components?.Count ?? 0}."
            );
        }
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

        Debug.Log("[ENGINE MINIGAME] ==========================");
        Debug.Log("[ENGINE MINIGAME] REINICIO DE EMERGENCIA INICIADO");
        Debug.Log("[ENGINE MINIGAME] Ronda 1");

        GenerateSequence();
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

        _currentInput = 0;
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

            ResetCurrentRound();
            return;
        }

        Debug.Log(
            $"[ENGINE MINIGAME] Correcto: " +
            $"{componentIndex}"
        );

        _currentInput++;

        if (_currentInput >= _currentSequence.Count)
        {
            CompleteRound();
        }
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

        Debug.Log(
            $"[ENGINE MINIGAME] Comenzando ronda {_currentRound}."
        );

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

        Debug.Log("[ENGINE MINIGAME] ==========================");
        Debug.Log("[ENGINE MINIGAME] REINICIO DE EMERGENCIA COMPLETADO");

        if (engineSystem != null)
        {
            engineSystem.RestartEngine();
        }
    }
}