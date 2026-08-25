using UnityEngine;
using UnityEngine.InputSystem;

public class LeverInteractionStation : MonoBehaviour, IPossessable
{
    [SerializeField]
    private MonoBehaviour controlledLever;

    public string MapName => stationMapName;
    

    [Header("Action Maps")]
    [SerializeField] private string playerMapName = "Player";
    [SerializeField] private string stationMapName = "Station";

    [Header("Input Actions")]
    [SerializeField] private string lookActionName = "Look";
    [SerializeField] private string exitActionName = "ExitStation";

    private PlayerCharacter _currentPlayer;

    private ILeverControls _leverControls;

    private void Awake()
    {
        _leverControls = controlledLever as ILeverControls;
        if (_leverControls == null) Log.Error($"{name}: Controlled Lever must implement ILeverControls.");
        enabled = false;
    }

    #region Possession

    public void Possess(PlayerCharacter player)
    {
        if (_currentPlayer != null) return;
        Log.Info("Lever Possessed");
        _currentPlayer = player;
        _currentPlayer.OnPossessionState(this);
        var currentMap = _currentPlayer.Input.currentActionMap;
        InputAction lookAction = currentMap.FindAction(lookActionName, true);
        InputAction exitAction = currentMap.FindAction(exitActionName, true);
        lookAction.performed += OnLookPerformed;
        exitAction.started += OnExitPerformed;
        enabled = true;
    }

    public void UnPossess()
    {
        if (_currentPlayer == null) return;
        var currentMap = _currentPlayer.Input.currentActionMap;
        InputAction lookAction = currentMap.FindAction(lookActionName, true);
        InputAction exitAction = currentMap.FindAction(exitActionName, true);
        lookAction.performed -= OnLookPerformed;
        exitAction.started -= OnExitPerformed;
        _currentPlayer.OnUnPossessionState(this);
        _currentPlayer = null;
        enabled = false;
        Log.Info("Lever Released");
    }

    #endregion

    #region Input

    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        if (_leverControls == null) return;
        Vector2 delta = context.ReadValue<Vector2>();
        Log.Info($"[LEVER] Dragging: {delta}");
        _leverControls.OnActionDrag(delta.y);
    }

    private void OnExitPerformed(InputAction.CallbackContext context)
    {
        Log.Info("Exit");
        UnPossess();
    }

    #endregion
}