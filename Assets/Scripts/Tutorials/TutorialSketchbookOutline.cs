using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Applies a pulsing outline material to the sketchbook's own mesh renderers.</summary>
[DisallowMultipleComponent]
public class TutorialSketchbookOutline : MonoBehaviour
{
    private const string SizePropertyName = "_Outline_Size";
    private const string StencilComparePropertyName = "_Outline_StencilCompare";

    [SerializeField] private Material outlineMaterial;
    [Header("Breathing")]
    [Tooltip("Minimum fraction of the outline width set in the material.")]
    [SerializeField, Range(0f, 1f)] private float minimumOutlineFraction = 0.4f;
    [SerializeField, Min(0.01f)] private float blinkFrequency = 1.5f;
    [Tooltip("Optional. Color property whose alpha also pulses (leave empty to skip).")]
    [SerializeField] private string colorPropertyName = "_Outline_Color";
    [SerializeField, Range(0f, 1f)] private float minimumAlpha = 0.25f;

    private sealed class RendererMaterials
    {
        public MeshRenderer Renderer;
        public MeshFilter Filter;
        public Mesh OriginalMesh;
        public Mesh OutlineMesh;
        public Material[] OriginalMaterials;
        public Material[] OutlineMaterials;
    }

    private readonly List<RendererMaterials> _renderers = new();
    private Material _runtimeOutlineMaterial;
    private Material _runtimeMaskMaterial;
    private float _visibleSince;
    private bool _isVisible;
    private bool _outlineApplied;

    private void OnDisable()
    {
        SetVisible(false);
    }

    private void OnDestroy()
    {
        SetVisible(false);
        ReleaseResources();
    }

    public void Initialize(Material material)
    {
        SetVisible(false);
        if (material != null && material != outlineMaterial)
        {
            ReleaseResources();
            outlineMaterial = material;
        }

        if (outlineMaterial == null || _runtimeOutlineMaterial != null) return;

        _runtimeOutlineMaterial = new Material(outlineMaterial);
        _runtimeOutlineMaterial.SetShaderPassEnabled("ShadowCaster", false);
        if (_runtimeOutlineMaterial.HasProperty(StencilComparePropertyName))
        {
            // Draw every unexpanded book surface into stencil before drawing the enlarged outline.
            _runtimeMaskMaterial = new Material(_runtimeOutlineMaterial);
            _runtimeMaskMaterial.renderQueue = _runtimeOutlineMaterial.renderQueue - 1;
            _runtimeMaskMaterial.SetFloat(SizePropertyName, 1f);
            _runtimeMaskMaterial.SetFloat("_Outline_Cull", (float)CullMode.Off);
            _runtimeMaskMaterial.SetFloat("_Outline_ColorMask", 0f);
            _runtimeMaskMaterial.SetFloat(StencilComparePropertyName, (float)CompareFunction.Always);
            _runtimeMaskMaterial.SetFloat("_Outline_StencilPass", (float)StencilOp.Replace);
        }
        PrepareRenderers();
    }

    public void SetVisible(bool isVisible)
    {
        if (isVisible == _isVisible || _runtimeOutlineMaterial == null) return;

        _isVisible = isVisible;
        if (_isVisible)
        {
            _visibleSince = Time.time;
            Update();
        }
        else
        {
            ApplyOutline(false);
        }
    }

    private void Update()
    {
        if (!_isVisible || _runtimeOutlineMaterial == null) return;

        float phase = (Mathf.Cos((Time.time - _visibleSince) * blinkFrequency * Mathf.PI * 2f) + 1f) * 0.5f;
        bool hasSize = outlineMaterial != null && outlineMaterial.HasProperty(SizePropertyName)
                       && _runtimeOutlineMaterial.HasProperty(SizePropertyName);
        if (hasSize)
        {
            float outlineSize = outlineMaterial.GetFloat(SizePropertyName);
            float widthFraction = Mathf.Lerp(minimumOutlineFraction, 1f, phase);
            _runtimeOutlineMaterial.SetFloat(SizePropertyName, Mathf.Lerp(1f, outlineSize, widthFraction));
        }

        bool hasColor = !string.IsNullOrEmpty(colorPropertyName) && _runtimeOutlineMaterial.HasProperty(colorPropertyName);
        if (hasColor)
        {
            Color color = _runtimeOutlineMaterial.GetColor(colorPropertyName);
            color.a = Mathf.Lerp(minimumAlpha, 1f, phase);
            _runtimeOutlineMaterial.SetColor(colorPropertyName, color);
        }

        ApplyOutline(hasSize || hasColor || phase > 0.35f);
    }

    private void PrepareRenderers()
    {
        foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!renderer.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null) continue;

            Mesh mesh = filter.sharedMesh;
            Material[] materials = renderer.sharedMaterials;
            int subMeshCount = mesh.subMeshCount;
            if (subMeshCount == 0 || materials.Length < subMeshCount) continue;
            if (subMeshCount > 1 && !mesh.isReadable)
            {
                Debug.LogWarning("Enable Read/Write on the sketchbook mesh to outline all its submeshes.", renderer);
                continue;
            }

            int maskSlots = _runtimeMaskMaterial != null ? subMeshCount : 0;
            int firstOutlineSlot = materials.Length + maskSlots;
            Material[] withOutline = new Material[firstOutlineSlot + subMeshCount];
            Array.Copy(materials, withOutline, materials.Length);
            Array.Fill(withOutline, _runtimeMaskMaterial, materials.Length, maskSlots);
            Array.Fill(withOutline, _runtimeOutlineMaterial, firstOutlineSlot, subMeshCount);

            _renderers.Add(new RendererMaterials
            {
                Renderer = renderer,
                Filter = filter,
                OriginalMesh = mesh,
                OutlineMesh = subMeshCount > 1 ? CreateOutlineMesh(mesh, materials.Length, withOutline.Length) : null,
                OriginalMaterials = materials,
                OutlineMaterials = withOutline
            });
        }
    }

    private static Mesh CreateOutlineMesh(Mesh source, int originalMaterialCount, int totalMaterialCount)
    {
        // Extra material slots otherwise redraw only the last submesh.
        int sourceSubMeshCount = source.subMeshCount;
        Mesh mesh = Instantiate(source);
        mesh.name = source.name + " (Outline)";
        mesh.subMeshCount = totalMaterialCount;

        for (int index = sourceSubMeshCount; index < mesh.subMeshCount; index++)
        {
            int originalIndex = index < originalMaterialCount
                ? sourceSubMeshCount - 1
                : (index - originalMaterialCount) % sourceSubMeshCount;
            mesh.SetIndices(source.GetIndices(originalIndex, applyBaseVertex: false), source.GetTopology(originalIndex),
                index, calculateBounds: false, baseVertex: source.GetSubMesh(originalIndex).baseVertex);
        }

        return mesh;
    }

    private void ApplyOutline(bool visible)
    {
        if (visible == _outlineApplied) return;
        _outlineApplied = visible;

        foreach (RendererMaterials entry in _renderers)
        {
            if (entry.Renderer == null) continue;
            if (entry.OutlineMesh != null && entry.Filter != null)
            {
                entry.Filter.sharedMesh = visible ? entry.OutlineMesh : entry.OriginalMesh;
            }
            entry.Renderer.sharedMaterials = visible ? entry.OutlineMaterials : entry.OriginalMaterials;
        }
    }

    private void ReleaseResources()
    {
        foreach (RendererMaterials entry in _renderers)
        {
            if (entry.OutlineMesh != null) Destroy(entry.OutlineMesh);
        }
        _renderers.Clear();

        if (_runtimeOutlineMaterial != null)
        {
            Destroy(_runtimeOutlineMaterial);
            _runtimeOutlineMaterial = null;
        }
        if (_runtimeMaskMaterial != null)
        {
            Destroy(_runtimeMaskMaterial);
            _runtimeMaskMaterial = null;
        }
    }
}
