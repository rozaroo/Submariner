using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseCanvas;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject controlsPanel;

    [Header("Input Settings")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private string pauseActionName = "Pause";
    private InputAction pauseAction;

    private bool _isPaused = false;
    private CursorLockMode _previousLockState;
    private bool _previousCursorVisible;

    private string _previousActionMap;

    private void Awake()
    {
        if (playerInput == null) playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput != null) pauseAction = playerInput.actions.FindAction("UI/" + pauseActionName);
    }

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.Enable();
            pauseAction.performed += OnPausePerformed;
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.performed -= OnPausePerformed;
            pauseAction.Disable();
        }
    }

    private void Start()
    {
        if (pauseCanvas != null) pauseCanvas.SetActive(false);
    }
    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (_isPaused)
        {
            if (controlsPanel != null && controlsPanel.activeSelf) ShowPausePanel();
            else Resume();
        }
        else
        {
            if (Time.timeScale == 0f) return;
            Pause();
        }
    }

    private bool IsPlayerInStation()
    {
        if (playerInput != null && playerInput.currentActionMap != null) return playerInput.currentActionMap.name == "Station";
        return false;
    }

    public void Resume()
    {
        pauseCanvas.SetActive(false);
        ShowPausePanel();
        Time.timeScale = 1f;
        _isPaused = false;
        // Restaurar el Action Map anterior
        if (!string.IsNullOrEmpty(_previousActionMap))
        {
            playerInput.SwitchCurrentActionMap(_previousActionMap);
            // Volver a habilitar Pause para la próxima pausa
            if (pauseAction != null) pauseAction.Enable();
        }
        // Restaurar cursor
        Cursor.lockState = _previousLockState;
        Cursor.visible = _previousCursorVisible;
        if (_previousLockState == CursorLockMode.Locked) StartCoroutine(ForceCursorLock());
    }

    private System.Collections.IEnumerator ForceCursorLock()
    {
        yield return null; // Wait one frame
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Pause()
    {
        if (playerInput == null) return;

        // Guardar el Action Map actual
        if (playerInput.currentActionMap != null) _previousActionMap = playerInput.currentActionMap.name;

        // Guardar el estado del cursor
        _previousLockState = Cursor.lockState;
        _previousCursorVisible = Cursor.visible;

        // Cambiar al mapa de pausa
        playerInput.SwitchCurrentActionMap("UI");

        ShowPausePanel();
        pauseCanvas.SetActive(true);
        Time.timeScale = 0f;
        _isPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ShowControls()
    {
        if (!_isPaused || pausePanel == null || controlsPanel == null) return;
        pausePanel.SetActive(false);
        controlsPanel.SetActive(true);
    }

    public void ShowPausePanel()
    {
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f; // Asegurarse de resetear el tiempo antes de cambiar escena
        SceneTransitionManager.Instance.LoadSceneWithFade("MainMenu");
    }

    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
