using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIPlayerManager : MonoBehaviour
{
    [Header("Interaction Prompt")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private Image keyIcon;
    [SerializeField] private TMP_Text promptText;

    [Header("Drop Prompt")]
    [SerializeField] private Sprite dropKeyIconSprite;
    private Sprite defaultKeyIconSprite;

    [Header("Prompt Text Alignment")]
    [SerializeField] private RectTransform promptTextRect;
    [SerializeField] private float shortTextOffset = 0f;
    [SerializeField] private float longTextOffset = -60f;
    [SerializeField] private int longTextLength = 25;
    private Vector2 defaultTextPosition;

    [Header("Detection Settings")]
    [SerializeField] private PlayerCharacter player;
    [SerializeField] private float interactionDistance = 2.5f;
    [SerializeField] private LayerMask interactableLayer;
    private bool _isUsingPeriscope;
    private bool _isUsingNavigation;
    private bool _isUsingDrainage;
    private bool _isUsingLeverStation;

    private void OnEnable()
    {
        GameEventChannel<OnPeriscopePossess>.OnEventRaised += HandlePeriscopePossess;
        GameEventChannel<OnPeriscopeUnPossess>.OnEventRaised += HandlePeriscopeUnPossess;
        NavigationStation.OnNavigationPossessionChanged += HandleNavigationPossession;
        DrainageStation.OnDrainagePossessionChanged += HandleDrainagePossession;
        LeverControlStation.OnLeverStationPossessionChanged += HandleLeverStationPossession;
    }
    private void OnDisable()
    {
        GameEventChannel<OnPeriscopePossess>.OnEventRaised -= HandlePeriscopePossess;
        GameEventChannel<OnPeriscopeUnPossess>.OnEventRaised -= HandlePeriscopeUnPossess;
        NavigationStation.OnNavigationPossessionChanged -= HandleNavigationPossession;
        DrainageStation.OnDrainagePossessionChanged -= HandleDrainagePossession;
        LeverControlStation.OnLeverStationPossessionChanged -= HandleLeverStationPossession;
    }
    private void HandleLeverStationPossession(bool isUsing)
    {
        _isUsingLeverStation = isUsing;
        if (isUsing) HidePrompt();
    }
    private void HandleDrainagePossession(bool isUsing)
    {
        _isUsingDrainage = isUsing;
        if (isUsing) HidePrompt();
    }
    private void HandleNavigationPossession(bool isUsing)
    {
        _isUsingNavigation = isUsing;
        if (isUsing) HidePrompt();
    }
    private void HandlePeriscopePossess(OnPeriscopePossess data)
    {
        _isUsingPeriscope = true;
        HidePrompt();
    }

    private void HandlePeriscopeUnPossess(OnPeriscopeUnPossess data)
    {
        _isUsingPeriscope = false;
    }
    private void Start()
    {
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        if (keyIcon != null) defaultKeyIconSprite = keyIcon.sprite;
        if (promptTextRect != null) defaultTextPosition = promptTextRect.anchoredPosition;
    }
    private void Update()
    {
        DetectInteractable();
    }

    private void DetectInteractable()
    {
        if (_isUsingPeriscope || _isUsingNavigation || _isUsingDrainage || _isUsingLeverStation)
        {
            HidePrompt();
            return;
        }
        if (player == null || player.CamController == null || player.CamController.MainCamera == null)
        {
            HidePrompt();
            return;
        }
        InventorySystem inventory = player.InventorySystem;
        // Si sostenemos un tanque/soplete, mostrar el prompt para soltarlo.
        if (inventory != null && (inventory.IsHolding<OxygenTank>() || inventory.IsHolding<Blowtorch>()))
        {
            ShowPrompt("Presione para soltar", dropKeyIconSprite);
            return;
        }
        Camera cam = player.CamController.MainCamera;
        Ray ray = new Ray(cam.transform.position,cam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactableLayer))
        {
            // Primero comprobamos si es una puerta.
            PressureDoor door = hit.collider.GetComponentInParent<PressureDoor>();

            if (door != null)
            {
                ShowPrompt(
                    door.IsOpen
                        ? "Presione para cerrar"
                        : "Presione para abrir"
                );
                return;
            }
            // Después comprobamos si es una estación de periscopio.
            PeriscopeStation periscope = hit.collider.GetComponentInParent<PeriscopeStation>();
            if (periscope != null)
            {
                ShowPrompt("Presione para usar el periscopio");
                return;
            }
            NavigationStation navigation = hit.collider.GetComponentInParent<NavigationStation>();
            if (navigation != null)
            {
                ShowPrompt("Presione para usar la navegación");
                return;
            }
            DrainageStation drainage = hit.collider.GetComponentInParent<DrainageStation>();
            if (drainage != null)
            {
                ShowPrompt("Presione para usar el drenaje");
                return;
            }
            LeverControlStation leverStation = hit.collider.GetComponentInParent<LeverControlStation>();
            if (leverStation != null)
            {
                ShowPrompt("Presione para usar los controles");
                return;
            }
            // Tanque de oxígeno recogible.
            OxygenTank oxygenTank = hit.collider.GetComponentInParent<OxygenTank>();

            if (oxygenTank != null && inventory != null && !inventory.HasHeldItem)
            {
                ShowPrompt("Presione para recoger");
                return;
            }
            Blowtorch blowtorch = hit.collider.GetComponentInParent<Blowtorch>();
            if (blowtorch != null && inventory != null && !inventory.HasHeldItem)
            {
                ShowPrompt("Presione para recoger");
                return;
            }
        }
        HidePrompt();
    }
    private void ShowPrompt(string message, Sprite icon = null)
    {
        if (interactionPrompt != null && !interactionPrompt.activeSelf) interactionPrompt.SetActive(true);
        if (promptText != null) promptText.text = message;
        if (keyIcon != null) keyIcon.sprite = icon != null ? icon : defaultKeyIconSprite;

        if (promptTextRect != null)
        {
            float offset = message.Length > longTextLength
                ? longTextOffset
                : shortTextOffset;

            promptTextRect.anchoredPosition = new Vector2(
                defaultTextPosition.x + offset,
                defaultTextPosition.y
            );
        }
    }
    private void HidePrompt()
    {
        if (interactionPrompt != null && interactionPrompt.activeSelf) interactionPrompt.SetActive(false);
    }
}

