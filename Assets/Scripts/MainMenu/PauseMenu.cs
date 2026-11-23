using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseCanvas;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject controlsPanel;

    private bool _isPaused = false;
    private CursorLockMode _previousLockState;
    private bool _previousCursorVisible;
    private PlayerInput _playerInput;
    private InputActionMap _pausedActionMap;

    private void Start()
    {
        if (pauseCanvas != null)
        {
            pauseCanvas.SetActive(false);
        }

        _playerInput = FindFirstObjectByType<PlayerInput>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isPaused)
            {
                if (controlsPanel != null && controlsPanel.activeSelf)
                    ShowPausePanel();
                else
                    Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    private bool IsPlayerInStation()
    {
        if (_playerInput == null)
        {
            _playerInput = FindFirstObjectByType<PlayerInput>();
        }

        if (_playerInput != null && _playerInput.currentActionMap != null)
        {
            return _playerInput.currentActionMap.name == "Station";
        }

        return false;
    }

    public void Resume()
    {
        if (!_isPaused) return;

        pauseCanvas.SetActive(false);
        ShowPausePanel();
        Time.timeScale = 1f;
        _isPaused = false;
        
        // Restore cursor state
        Cursor.lockState = _previousLockState;
        Cursor.visible = _previousCursorVisible;
        _pausedActionMap?.Enable();
        _pausedActionMap = null;

        // Force cursor lock in editor if it was locked
        if (_previousLockState == CursorLockMode.Locked)
        {
            StartCoroutine(ForceCursorLock());
        }
    }

    private System.Collections.IEnumerator ForceCursorLock()
    {
        yield return null; // Wait one frame
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Pause()
    {
        if (_isPaused || Time.timeScale == 0f || IsPlayerInStation()) return;

        // Store current cursor state before pausing
        _previousLockState = Cursor.lockState;
        _previousCursorVisible = Cursor.visible;

        Time.timeScale = 0f;
        _isPaused = true;

        // Input actions run independently of Time.timeScale.
        InputActionMap actionMap = _playerInput != null ? _playerInput.currentActionMap : null;
        if (actionMap != null && actionMap.enabled)
        {
            _pausedActionMap = actionMap;
            _pausedActionMap.Disable();
        }

        ShowPausePanel();
        pauseCanvas.SetActive(true);

        // Show cursor for menu navigation
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
