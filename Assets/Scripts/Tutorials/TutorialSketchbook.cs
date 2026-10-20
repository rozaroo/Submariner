using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Add this component to a sketchbook model placed in the scene. Assign its
/// station in the Inspector, or leave it empty to use the closest station.
/// </summary>
public class TutorialSketchbook : MonoBehaviour
{
    private const string StationClickActionName = "ClickInteraction";

    [Header("Station")]
    [Tooltip("Optional. Assign the station that owns this sketchbook. If empty, the nearest IPossessable station is used.")]
    [SerializeField] private MonoBehaviour owningStation;
    [Tooltip("Collider used to receive clicks. The component never modifies this collider.")]
    [SerializeField] private Collider clickCollider;

    [Header("Reading placement")]
    [SerializeField, Min(0.01f)] private float readingDistance = 0.65f;
    [SerializeField] private Vector3 readingOffset = new(0f, -0.05f, 0f);
    [Tooltip("Extra local rotation relative to the camera when reading. X tilts, Y turns, and Z rolls the sketchbook.")]
    [SerializeField] private Vector3 readingRotationOffset;
    [SerializeField, Min(0.01f)] private float transitionDuration = 0.25f;

    private IPossessable _owningStation;
    private Collider _sketchbookCollider;
    private TutorialSketchbookContent _content;
    private Camera _stationCamera;
    private InputAction _clickAction;
    private Coroutine _transition;
    private bool _isReading;
    private Vector3 _tableWorldPosition;
    private Quaternion _tableWorldRotation;

    private void Awake()
    {
        CacheOwningStation();
        CacheTablePose();
        CacheClickCollider();
        _content = GetComponent<TutorialSketchbookContent>();
    }

    private void OnEnable()
    {
        PlayerCharacter.StationPossessed += HandleStationPossessed;
        PlayerCharacter.StationUnpossessed += HandleStationUnpossessed;
    }

    private void OnDisable()
    {
        StopListeningForClicks();
        PlayerCharacter.StationPossessed -= HandleStationPossessed;
        PlayerCharacter.StationUnpossessed -= HandleStationUnpossessed;
    }

    private void CacheOwningStation()
    {
        if (owningStation is IPossessable configuredStation)
        {
            _owningStation = configuredStation;
            return;
        }

        foreach (MonoBehaviour component in GetComponentsInParent<MonoBehaviour>())
        {
            if (component is IPossessable station)
            {
                _owningStation = station;
                return;
            }
        }

        float closestDistanceSquared = float.MaxValue;
        foreach (MonoBehaviour component in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (component is not IPossessable station)
            {
                continue;
            }

            float distanceSquared = (component.transform.position - transform.position).sqrMagnitude;
            if (distanceSquared < closestDistanceSquared)
            {
                closestDistanceSquared = distanceSquared;
                _owningStation = station;
            }
        }

        if (_owningStation == null)
        {
            Log.Warning("[TutorialSketchbook] Assign an IPossessable station in the Inspector.");
        }
    }

    private void CacheTablePose()
    {
        _tableWorldPosition = transform.position;
        _tableWorldRotation = transform.rotation;
    }

    private void HandleStationPossessed(IPossessable station, PlayerCharacter player)
    {
        if (station != _owningStation || player == null || player.CamController == null || player.Input == null)
        {
            return;
        }

        _stationCamera = player.CamController.MainCamera;
        StopListeningForClicks();
        _clickAction = player.Input.actions[StationClickActionName];
        StationClickBlocker.Register(this);

        if (_clickAction != null)
        {
            _clickAction.started += OnClickStarted;
        }
    }

    private void HandleStationUnpossessed(IPossessable station, PlayerCharacter player)
    {
        if (station != _owningStation)
        {
            return;
        }

        StopListeningForClicks();
        _stationCamera = null;
        ReturnToTable(immediately: true);
    }

    private void CacheClickCollider()
    {
        if (clickCollider == null)
        {
            clickCollider = GetComponent<Collider>();
        }

        if (clickCollider == null)
        {
            clickCollider = GetComponentInChildren<Collider>();
        }

        _sketchbookCollider = clickCollider;
        if (_sketchbookCollider == null)
        {
            Log.Warning("[TutorialSketchbook] Assign a collider in the Inspector to receive clicks.");
        }
    }

    private void OnClickStarted(InputAction.CallbackContext context)
    {
        StationClickBlocker.TryConsumeClick(_stationCamera);
    }

    internal bool TryConsumeStationClick(Camera stationCamera)
    {
        if (_stationCamera != stationCamera || _sketchbookCollider == null || Mouse.current == null)
        {
            return false;
        }

        if (_isReading)
        {
            if (IsPointerOverSketchbook())
            {
                ReturnToTable(immediately: false);
            }

            return true;
        }

        if (!IsPointerOverSketchbook())
        {
            return false;
        }

        BringToReadingPosition();
        return true;
    }

    private bool IsPointerOverSketchbook()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector2 viewportPosition = new(mousePosition.x / Screen.width, mousePosition.y / Screen.height);
        Ray ray = _stationCamera.ViewportPointToRay(viewportPosition);

        // Test the book directly so the table collider cannot intercept it.
        return _sketchbookCollider.Raycast(ray, out _, 5f) || RayHitsVisualBounds(ray);
    }

    private void BringToReadingPosition()
    {
        _isReading = true;
        _content?.Show();
        Vector3 position = _stationCamera.transform.TransformPoint(readingOffset + Vector3.forward * readingDistance);
        Quaternion rotation = _stationCamera.transform.rotation
                              * Quaternion.Euler(0f, 180f, 0f)
                              * Quaternion.Euler(90f, 0f, 0f)
                              * Quaternion.Euler(readingRotationOffset);
        StartTransition(position, rotation);
    }

    private void ReturnToTable(bool immediately)
    {
        _isReading = false;
        _content?.Hide();

        if (immediately)
        {
            StopTransition();
            transform.SetPositionAndRotation(_tableWorldPosition, _tableWorldRotation);
            return;
        }

        StartTransition(_tableWorldPosition, _tableWorldRotation);
    }

    private void StartTransition(Vector3 targetPosition, Quaternion targetRotation)
    {
        StopTransition();
        _transition = StartCoroutine(AnimateTo(targetPosition, targetRotation));
    }

    private void StopTransition()
    {
        if (_transition == null)
        {
            return;
        }

        StopCoroutine(_transition);
        _transition = null;
    }

    private IEnumerator AnimateTo(Vector3 targetPosition, Quaternion targetRotation)
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
            transform.SetPositionAndRotation(
                Vector3.Lerp(startPosition, targetPosition, t),
                Quaternion.Slerp(startRotation, targetRotation, t));
            yield return null;
        }

        transform.SetPositionAndRotation(targetPosition, targetRotation);
        _transition = null;
    }

    private void StopListeningForClicks()
    {
        StationClickBlocker.Unregister(this);

        if (_clickAction != null)
        {
            _clickAction.started -= OnClickStarted;
            _clickAction = null;
        }
    }

    private bool RayHitsVisualBounds(Ray ray)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return false;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds.IntersectRay(ray, out float distance) && distance <= 5f;
    }

}
