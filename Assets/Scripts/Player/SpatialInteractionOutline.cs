using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Additional mesh draws using the supplied outline material; original materials stay intact.</summary>
internal sealed class SpatialInteractionOutline
{
    private sealed class Entry
    {
        public Renderer renderer;
        public MeshFilter filter;
        public SkinnedMeshRenderer skinned;
        public Mesh bakedMesh;
    }

    private readonly List<Entry> entries = new();
    private SpatialInteractionTarget target;
    private Material suppliedMaterial;
    private Shader suppliedShader;
    private Material drawMaterial;
    private bool ownsMaterial;

    public void SetFocus(SpatialInteractionTarget next, Material material, Shader shader)
    {
        if (next == null || !next.isActiveAndEnabled || !next.AllowOutline
            || next.Source == null || !next.Source.gameObject.activeInHierarchy
            || (material == null && shader == null))
        {
            Clear();
            return;
        }
        if (target == next && suppliedMaterial == material && suppliedShader == shader) return;

        Clear();
        target = next;
        suppliedMaterial = material;
        suppliedShader = shader;
        ownsMaterial = material == null;
        drawMaterial = ownsMaterial ? new Material(shader) { name = "Interaction outline (runtime)" } : material;

        var unique = new HashSet<Renderer>();
        foreach (var renderer in next.GetOutlineRenderers())
        {
            if (renderer == null || !unique.Add(renderer)) continue;
            if (renderer is SkinnedMeshRenderer skinned)
            {
                entries.Add(new Entry
                {
                    renderer = renderer, skinned = skinned,
                    bakedMesh = new Mesh { name = "Interaction outline skinned mesh" }
                });
            }
            else if (renderer is MeshRenderer && renderer.TryGetComponent(out MeshFilter filter))
                entries.Add(new Entry { renderer = renderer, filter = filter });
        }
    }

    public void Draw(Camera camera)
    {
        if (target == null || drawMaterial == null || camera == null) return;
        foreach (var entry in entries)
        {
            if (entry.renderer == null || !entry.renderer.enabled || entry.renderer.forceRenderingOff
                || !entry.renderer.gameObject.activeInHierarchy) continue;
            Mesh mesh;
            if (entry.skinned != null)
            {
                if (entry.skinned.sharedMesh == null) continue;
                entry.skinned.BakeMesh(entry.bakedMesh);
                mesh = entry.bakedMesh;
            }
            else mesh = entry.filter != null ? entry.filter.sharedMesh : null;
            if (mesh == null) continue;

            // Outline every submesh, including tables that use several base materials.
            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                Graphics.DrawMesh(mesh, entry.renderer.localToWorldMatrix, drawMaterial,
                    entry.renderer.gameObject.layer, camera, submesh, null,
                    ShadowCastingMode.Off, false, null, LightProbeUsage.Off);
        }
    }

    public void Clear()
    {
        foreach (var entry in entries)
            if (entry.bakedMesh != null) Object.Destroy(entry.bakedMesh);
        entries.Clear();
        if (ownsMaterial && drawMaterial != null) Object.Destroy(drawMaterial);
        target = null;
        suppliedMaterial = null;
        suppliedShader = null;
        drawMaterial = null;
        ownsMaterial = false;
    }
}
