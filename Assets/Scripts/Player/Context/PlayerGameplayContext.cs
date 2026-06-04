using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGameplayContext
{
    public PlayerCharacter Player { get; private set; }
    public CameraController CamController { get; private set; }
    public InventorySystem InventorySystem { get; private set; }
    public PlayerInput Input { get; private set; }
    public Camera MainCamera { get; private set; }
    
    public float InteractionDistance { get; private set; }
    public LayerMask InteractableLayer { get; private set; }
    
    public string InteractionActionName { get; private set; }
    public string DropActionName { get; private set; }
    public string UseActionName { get; private set; }

    public PlayerGameplayContext(
        PlayerCharacter player,
        CameraController camController,
        InventorySystem inventorySystem,
        PlayerInput input,
        float interactionDistance,
        LayerMask interactableLayer,
        string interactionActionName,
        string dropActionName,
        string useActionName)
    {
        Player = player;
        CamController = camController;
        InventorySystem = inventorySystem;
        Input = input;
        MainCamera = camController.MainCamera;
        InteractionDistance = interactionDistance;
        InteractableLayer = interactableLayer;
        InteractionActionName = interactionActionName;
        DropActionName = dropActionName;
        UseActionName = useActionName;
    }
}