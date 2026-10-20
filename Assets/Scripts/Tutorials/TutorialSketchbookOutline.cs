using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draws a separate copy of each mesh with an outline material. The original
/// renderers and their material slots are never modified.
/// </summary>
[DisallowMultipleComponent]
public class TutorialSketchbookOutline : MonoBehaviour
{
    private const string ProxyNamePrefix = "__TutorialSketchbookOutline_";

    [SerializeField] private Material outlineMaterial;
    [Header("Breathing")]
    [SerializeField, Min(0f)] private float minimumOutlineSize = 1.008f;
    [SerializeField, Min(0f)] private float maximumOutlineSize = 1.012f;
    [SerializeField, Min(0.01f)] private float breathingFrequency = 1.1f;

    private readonly List<GameObject> _proxies = new();
    private Material _runtimeOutlineMaterial;
    private bool _isVisible;

    private void Awake()
    {
        SetVisible(false);
    }

    private void OnDisable()
    {
        SetVisible(false);
    }

    private void OnDestroy()
    {
        ClearProxies();

        if (_runtimeOutlineMaterial != null)
        {
            Destroy(_runtimeOutlineMaterial);
        }
    }

    public void Initialize(Material material)
    {
        if (material != null && material != outlineMaterial)
        {
            outlineMaterial = material;
            ClearProxies();

            if (_runtimeOutlineMaterial != null)
            {
                Destroy(_runtimeOutlineMaterial);
                _runtimeOutlineMaterial = null;
            }
        }

        RemovePreviousOutlineProxies();

        if (outlineMaterial != null && _runtimeOutlineMaterial == null)
        {
            _runtimeOutlineMaterial = new Material(outlineMaterial);
            BuildProxies();
        }

        SetVisible(false);
    }

    public void SetVisible(bool isVisible)
    {
        _isVisible = isVisible;

        foreach (GameObject proxy in _proxies)
        {
            if (proxy != null)
            {
                proxy.SetActive(isVisible);
            }
        }
    }

    private void Update()
    {
        if (!_isVisible || _runtimeOutlineMaterial == null || !_runtimeOutlineMaterial.HasProperty("_Outline_Size"))
        {
            return;
        }

        float pulse = (Mathf.Sin(Time.time * breathingFrequency * Mathf.PI * 2f) + 1f) * 0.5f;
        _runtimeOutlineMaterial.SetFloat("_Outline_Size", Mathf.Lerp(minimumOutlineSize, maximumOutlineSize, pulse));
    }

    private void BuildProxies()
    {
        MeshRenderer[] sourceRenderers = GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer sourceRenderer in sourceRenderers)
        {
            if (sourceRenderer.GetComponent<TutorialSketchbookOutlineProxy>() != null)
            {
                continue;
            }

            MeshFilter sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
            {
                continue;
            }

            GameObject proxy = new GameObject($"{ProxyNamePrefix}{sourceRenderer.GetInstanceID()}");
            proxy.layer = sourceRenderer.gameObject.layer;
            proxy.AddComponent<TutorialSketchbookOutlineProxy>();

            Transform proxyTransform = proxy.transform;
            proxyTransform.SetParent(sourceRenderer.transform.parent, false);
            proxyTransform.localPosition = sourceRenderer.transform.localPosition;
            proxyTransform.localRotation = sourceRenderer.transform.localRotation;
            proxyTransform.localScale = sourceRenderer.transform.localScale;

            MeshFilter proxyFilter = proxy.AddComponent<MeshFilter>();
            proxyFilter.sharedMesh = sourceFilter.sharedMesh;

            MeshRenderer proxyRenderer = proxy.AddComponent<MeshRenderer>();
            proxyRenderer.sharedMaterial = _runtimeOutlineMaterial;
            proxyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            proxyRenderer.receiveShadows = false;
            _proxies.Add(proxy);
        }
    }

    private void ClearProxies()
    {
        foreach (GameObject proxy in _proxies)
        {
            if (proxy != null)
            {
                Destroy(proxy);
            }
        }

        _proxies.Clear();
    }

    private void RemovePreviousOutlineProxies()
    {
        bool hasCurrentProxies = _proxies.Count > 0;

        foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>(true))
        {
            bool isCurrentProxy = !hasCurrentProxies && renderer.GetComponent<TutorialSketchbookOutlineProxy>() != null;
            bool isOldProxy = renderer.gameObject.name.EndsWith(" Outline") && outlineMaterial != null &&
                              renderer.sharedMaterial != null && renderer.sharedMaterial.shader == outlineMaterial.shader;

            if (isCurrentProxy || isOldProxy)
            {
                Destroy(renderer.gameObject);
            }
        }
    }
}

/// <summary>Marks a renderer generated exclusively for the sketchbook outline.</summary>
public class TutorialSketchbookOutlineProxy : MonoBehaviour { }
