using UnityEngine;

public class EngineMiniGameActivator : MonoBehaviour, IPossessable, IInteractable
{
    [Header("References")]
    [SerializeField] private EngineMiniGame engineMiniGame;

    [Header("Possession Config")]
    [SerializeField] private Transform cameraAnchor;
    [SerializeField] private Transform directionAnchor;
    [SerializeField] private float transitionDuration = 0.1f;
    [SerializeField] private CursorLockMode cursorLockMode;
    [SerializeField] private bool showMouseCursor;

    [Header("Actions Maps Settings")]
    [SerializeField] private string stationMapName;

    private bool _playerInside;

    public string MapName => stationMapName;
    public Transform CameraAnchor => cameraAnchor;
    public Transform DirectionAnchor => directionAnchor;
    public float TransitionDuration => transitionDuration;
    public CursorLockMode CursorLockMode => cursorLockMode;
    public bool IsMouseVisible => showMouseCursor;

    public void Activate(PlayerCharacter player)
    {
        if (engineMiniGame == null)
        {
            Debug.LogError("[ENGINE MINIGAME] EngineMiniGame no está asignado.");
            return;
        }

        engineMiniGame.StartMinigame();
    }

    public void Interact(PlayerCharacter player)
    {
        player.OnPossessionState(this);
    }
    public void Possess(PlayerCharacter player)
    {
        Activate(player);
    }

    public void UnPossess()
    {
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerCharacter>(out _))
            _playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<PlayerCharacter>(out _))
            _playerInside = false;
    }
}
