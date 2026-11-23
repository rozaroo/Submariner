using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public sealed class PickupOutlineFeature : ScriptableRendererFeature
{
    private PickupOutlinePass pass;

    public override void Create() => pass = new PickupOutlinePass();

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        Camera camera = renderingData.cameraData.camera;
        if (renderingData.cameraData.cameraType != CameraType.Game) return;

        var interaction = camera.GetComponentInParent<SpatialInteractionSystem>();
        if (interaction == null || !interaction.HasPickupOutlines(camera)) return;

        pass.SetInteraction(interaction);
        renderer.EnqueuePass(pass);
    }

    private sealed class PickupOutlinePass : ScriptableRenderPass
    {
        private static readonly int MaskTexture = Shader.PropertyToID("_PickupOutlineMask");
        private readonly List<MeshRenderer> pickupRenderers = new();
        private readonly MaterialPropertyBlock properties = new();
        private SpatialInteractionSystem interaction;

        private sealed class MaskData
        {
            internal readonly List<(MeshRenderer renderer, int subMeshCount)> meshes = new();
            internal Material material;
        }

        private sealed class OutlineData
        {
            internal TextureHandle mask;
            internal Material material;
            internal MaterialPropertyBlock properties;
        }

        internal PickupOutlinePass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            requiresIntermediateTexture = true;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        internal void SetInteraction(SpatialInteractionSystem value) => interaction = value;

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            if (!resources.cameraDepthTexture.IsValid() || !resources.activeColorTexture.IsValid()) return;

            Camera camera = frameData.Get<UniversalCameraData>().camera;
            if (interaction == null || !interaction.HasPickupOutlines(camera)) return;

            var descriptor = renderGraph.GetTextureDesc(resources.activeColorTexture);
            descriptor.name = "Pickup Silhouette Depth";
            descriptor.format = GraphicsFormat.R16_SFloat;
            descriptor.msaaSamples = MSAASamples.None;
            descriptor.bindTextureMS = false;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.clearBuffer = true;
            descriptor.clearColor = Color.clear;
            TextureHandle mask = renderGraph.CreateTexture(descriptor);

            using (var builder = renderGraph.AddRasterRenderPass<MaskData>("Pickup Silhouette Mask", out var data))
            {
                data.material = interaction.PickupOutlineMaterial;
                data.meshes.Clear();
                foreach (GameObject pickup in interaction.OutlinedPickups)
                {
                    if (pickup == null || !pickup.activeInHierarchy) continue;
                    pickup.GetComponentsInChildren(false, pickupRenderers);
                    foreach (MeshRenderer renderer in pickupRenderers)
                    {
                        if (renderer == null || !renderer.enabled || renderer.forceRenderingOff
                            || (camera.cullingMask & (1 << renderer.gameObject.layer)) == 0
                            || !renderer.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
                            continue;

                        data.meshes.Add((renderer, filter.sharedMesh.subMeshCount));
                    }
                }
                pickupRenderers.Clear();

                builder.UseTexture(resources.cameraDepthTexture, AccessFlags.Read);
                builder.SetRenderAttachment(mask, 0, AccessFlags.Write);
                builder.SetRenderFunc(static (MaskData passData, RasterGraphContext context) =>
                {
                    foreach (var (renderer, subMeshCount) in passData.meshes)
                        for (int subMesh = 0; subMesh < subMeshCount; subMesh++)
                            context.cmd.DrawRenderer(renderer, passData.material, subMesh, 0);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<OutlineData>("Pickup Silhouette Outline", out var data))
            {
                data.mask = mask;
                data.material = interaction.PickupOutlineMaterial;
                data.properties = properties;
                builder.UseTexture(mask, AccessFlags.Read);
                builder.UseTexture(resources.cameraDepthTexture, AccessFlags.Read);
                builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                builder.SetRenderFunc(static (OutlineData passData, RasterGraphContext context) =>
                {
                    passData.properties.SetTexture(MaskTexture, passData.mask);
                    context.cmd.DrawProcedural(Matrix4x4.identity, passData.material, 1,
                        MeshTopology.Triangles, 3, 1, passData.properties);
                });
            }
        }
    }
}
