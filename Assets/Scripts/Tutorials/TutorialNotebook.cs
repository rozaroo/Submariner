using System;
using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class TutorialNotebook : MonoBehaviour
{
    private enum NotebookState
    {
        Resting,
        Opening,
        Reading,
        Closing
    }

    [Header("Identity and content")]
    [SerializeField, Tooltip("Unique, stable identifier used to persist whether this specific book was read.")]
    private string notebookId = "workbench-tutorial";

    [SerializeField, TextArea(3, 10)]
    private string tutorialText;

    [SerializeField]
    private Texture2D[] tutorialPages = Array.Empty<Texture2D>();

    [Header("Book animation")]
    [SerializeField, Tooltip("Leave empty to animate this object's transform.")]
    private Transform bookVisual;

    [SerializeField, Tooltip("Position relative to the table camera while the tutorial is open.")]
    private Vector3 openLocalPosition = new(0f, -0.05f, 0.55f);

    [SerializeField, Tooltip("Rotation relative to the table camera while the tutorial is open.")]
    private Vector3 openLocalEulerAngles = new(0f, 180f, 0f);

    [SerializeField, Min(0.01f)]
    private float openDuration = 0.2f;

    [SerializeField]
    private AnimationCurve openCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField, Min(0.01f)]
    private float openScaleMultiplier = 1f;

    [Header("Optional page display")]
    [SerializeField, Tooltip("Optional renderer whose material receives the selected tutorial page texture.")]
    private Renderer pageRenderer;

    [SerializeField]
    private string pageTextureProperty = "_BaseMap";

    [SerializeField, Tooltip("Optional TextMeshPro label shown while the notebook is open.")]
    private TMP_Text tutorialTextLabel;

    [Header("Outline feedback")]
    [SerializeField, Tooltip("Assign an outlined child object or a renderer using a transparent outline material.")]
    private GameObject outlineObject;

    [SerializeField]
    private Renderer outlineRenderer;

    [SerializeField]
    private string outlineColorProperty = "_Color";

    [SerializeField]
    private Color outlineColor = Color.yellow;

    [SerializeField, Range(0f, 1f)]
    private float unreadMinimumAlpha = 0.5f;

    [SerializeField, Min(0.01f)]
    private float unreadPulseSeconds = 1.1f;

    [Header("Audio (optional Wwise events)")]
    [SerializeField]
    private string hoverSfxEvent;

    [SerializeField]
    private string openSfxEvent;

    [SerializeField]
    private string closeSfxEvent;

    private readonly MaterialPropertyBlock _outlinePropertyBlock = new();

    private Coroutine _animationCoroutine;

    private Transform _bookVisual;

    private Vector3 _restPosition;
    private Quaternion _restRotation;
    private Vector3 _restScale;

    private bool _isRead;
    private bool _isHovered;

    private int _pageIndex;

    private NotebookState _state = NotebookState.Resting;

    public string NotebookId => notebookId;

    public string TutorialText => tutorialText;

    public Texture2D[] TutorialPages => tutorialPages;

    public int CurrentPageIndex => _pageIndex;

    public bool HasBeenRead => _isRead;


    public bool IsReading =>
        _state == NotebookState.Reading ||
        _state == NotebookState.Opening;

    private string SaveKey =>
        $"TutorialNotebook.{notebookId}.Read";

    private void Awake()
    {
        _bookVisual = bookVisual != null
            ? bookVisual
            : transform;

        _restPosition = _bookVisual.position;
        _restRotation = _bookVisual.rotation;
        _restScale = _bookVisual.localScale;

        _isRead = PlayerPrefs.GetInt(SaveKey, 0) == 1;

        ApplyPage();
        RefreshOutline();
    }

    private void Update()
    {
        RefreshOutline();
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(notebookId))
            notebookId = gameObject.name;

        unreadMinimumAlpha = Mathf.Clamp01(unreadMinimumAlpha);

        openDuration = Mathf.Max(
            0.01f,
            openDuration
        );

        openScaleMultiplier = Mathf.Max(
            0.01f,
            openScaleMultiplier
        );

        unreadPulseSeconds = Mathf.Max(
            0.01f,
            unreadPulseSeconds
        );
    }

    public void SetHovered(bool isHovered)
    {
        bool wasHovered = _isHovered;

        _isHovered =
            _state == NotebookState.Resting &&
            isHovered;

        if (_isHovered && !wasHovered)
        {
            PostSfx(hoverSfxEvent);
        }
    }


    public void Open(Camera tableCamera)
    {
        if (tableCamera == null)
            return;

        if (_state != NotebookState.Resting)
            return;

        _state = NotebookState.Opening;

        _isHovered = false;

        _pageIndex = 0;

        ApplyPage();

        PostSfx(openSfxEvent);

        StartBookAnimation(
            true,
            tableCamera.transform
        );
    }


    public void Close()
    {
        if (_state != NotebookState.Reading)
            return;

        _state = NotebookState.Closing;

        PostSfx(closeSfxEvent);

        StartBookAnimation(
            false,
            null
        );
    }

    public void CancelPresentation()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
        }

        _animationCoroutine = null;

        RestoreBookImmediately();

        _isHovered = false;

        _state = NotebookState.Resting;

        RefreshOutline();
    }

    public void NextPage()
    {
        if (!IsReading)
            return;

        if (tutorialPages == null ||
            tutorialPages.Length == 0)
            return;

        _pageIndex =
            (_pageIndex + 1) %
            tutorialPages.Length;

        ApplyPage();
    }

    public void PreviousPage()
    {
        if (!IsReading)
            return;

        if (tutorialPages == null ||
            tutorialPages.Length == 0)
            return;

        _pageIndex =
            (_pageIndex - 1 + tutorialPages.Length) %
            tutorialPages.Length;

        ApplyPage();
    }

    public bool OwnsCollider(Collider collider)
    {
        return collider != null &&
               collider.GetComponentInParent<TutorialNotebook>() == this;
    }

    [ContextMenu("Reset Saved Read State")]
    public void ResetReadState()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();

        _isRead = false;

        RefreshOutline();
    }

    private void StartBookAnimation(
        bool opening,
        Transform cameraTransform
    )
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
        }

        _animationCoroutine =
            StartCoroutine(
                AnimateBook(
                    opening,
                    cameraTransform
                )
            );
    }

    private IEnumerator AnimateBook(
        bool opening,
        Transform cameraTransform
    )
    {
        Vector3 startPosition =
            _bookVisual.position;

        Quaternion startRotation =
            _bookVisual.rotation;

        Vector3 startScale =
            _bookVisual.localScale;

        Vector3 targetPosition =
            opening
                ? cameraTransform.TransformPoint(
                    openLocalPosition
                )
                : _restPosition;

        Quaternion targetRotation =
            opening
                ? cameraTransform.rotation *
                  Quaternion.Euler(
                      openLocalEulerAngles
                  )
                : _restRotation;

        Vector3 targetScale =
            opening
                ? _restScale *
                  openScaleMultiplier
                : _restScale;

        float elapsed = 0f;

        while (elapsed < openDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / openDuration
                );

            t = openCurve.Evaluate(t);

            _bookVisual.SetPositionAndRotation(
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    t
                ),
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    t
                )
            );

            _bookVisual.localScale =
                Vector3.Lerp(
                    startScale,
                    targetScale,
                    t
                );

            yield return null;
        }

        _bookVisual.SetPositionAndRotation(
            targetPosition,
            targetRotation
        );

        _bookVisual.localScale =
            targetScale;

        _animationCoroutine = null;

        if (opening)
        {
            _state = NotebookState.Reading;
            yield break;
        }

        _isRead = true;

        PlayerPrefs.SetInt(
            SaveKey,
            1
        );

        PlayerPrefs.Save();

        _state = NotebookState.Resting;

        RefreshOutline();
    }

    private void RestoreBookImmediately()
    {
        if (_bookVisual == null)
            return;

        _bookVisual.SetPositionAndRotation(
            _restPosition,
            _restRotation
        );

        _bookVisual.localScale =
            _restScale;
    }

    private void RefreshOutline()
    {
        bool shouldShow =
            _state == NotebookState.Resting &&
            (!_isRead || _isHovered);

        if (outlineObject != null &&
            outlineObject.activeSelf != shouldShow)
        {
            outlineObject.SetActive(
                shouldShow
            );
        }

        if (!shouldShow ||
            outlineRenderer == null)
        {
            return;
        }

        float alpha =
            _isRead
                ? 1f
                : Mathf.Lerp(
                    unreadMinimumAlpha,
                    1f,
                    (
                        Mathf.Sin(
                            Time.unscaledTime *
                            Mathf.PI *
                            2f /
                            unreadPulseSeconds
                        ) + 1f
                    ) * 0.5f
                );

        Color color = outlineColor;

        color.a *= alpha;

        outlineRenderer.GetPropertyBlock(
            _outlinePropertyBlock
        );

        _outlinePropertyBlock.SetColor(
            outlineColorProperty,
            color
        );

        outlineRenderer.SetPropertyBlock(
            _outlinePropertyBlock
        );
    }

    private void ApplyPage()
    {
        if (tutorialTextLabel != null)
        {
            tutorialTextLabel.text =
                tutorialText;
        }

        if (pageRenderer == null)
            return;

        if (tutorialPages == null ||
            tutorialPages.Length == 0)
        {
            return;
        }

        _pageIndex =
            Mathf.Clamp(
                _pageIndex,
                0,
                tutorialPages.Length - 1
            );

        MaterialPropertyBlock block =
            new();

        pageRenderer.GetPropertyBlock(
            block
        );

        block.SetTexture(
            pageTextureProperty,
            tutorialPages[_pageIndex]
        );

        pageRenderer.SetPropertyBlock(
            block
        );
    }

    private void PostSfx(string eventName)
    {
        if (!string.IsNullOrWhiteSpace(eventName))
        {
            SFXManager.PostEvent(
                eventName,
                gameObject
            );
        }
    }
}