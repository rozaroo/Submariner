using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public sealed class UnderwaterBlurFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader blurShader;

    private Material material;
    private BlurPass pass;

    public override void Create()
    {
        CoreUtils.Destroy(material);
        material = blurShader ? CoreUtils.CreateEngineMaterial(blurShader) : null;
        pass = material ? new BlurPass(material) : null;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        Camera camera = renderingData.cameraData.camera;
        if (pass == null || renderingData.cameraData.cameraType != CameraType.Game
            || !camera.TryGetComponent(out UnderwaterView view) || !view.TryGetEffect(out Vector3 parameters))
            return;

        pass.SetParameters(parameters, view.OceanColor, view.HazeDensity);
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing) => CoreUtils.Destroy(material);

    private sealed class BlurPass : ScriptableRenderPass
    {
        private static readonly int UnderwaterParams = Shader.PropertyToID("_UnderwaterParams");
        private static readonly int OceanColor = Shader.PropertyToID("_OceanColor");
        private static readonly int PixelSize = Shader.PropertyToID("_UnderwaterPixelSize");
        private static readonly int BlitTexture = Shader.PropertyToID("_BlitTexture");
        private static readonly int DepthTexture = Shader.PropertyToID("_CameraDepthTexture");
        private static readonly int BlitScaleBias = Shader.PropertyToID("_BlitScaleBias");
        private readonly Material material;
        private readonly MaterialPropertyBlock properties = new();
        private Vector4 effectParameters;
        private Color oceanColor;

        private sealed class PassData
        {
            internal TextureHandle source;
            internal TextureHandle depth;
            internal Material material;
            internal MaterialPropertyBlock properties;
            internal Vector4 effect;
            internal Vector4 pixelSize;
            internal Color oceanColor;
        }

        internal BlurPass(Material material)
        {
            this.material = material;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            requiresIntermediateTexture = true;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        internal void SetParameters(Vector3 parameters, Color color, float density)
        {
            effectParameters = new Vector4(parameters.x, parameters.y, parameters.z, density);
            oceanColor = color;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            if (resources.isActiveTargetBackBuffer || !resources.activeColorTexture.IsValid()
                || !resources.cameraDepthTexture.IsValid())
                return;

            TextureHandle source = resources.activeColorTexture;
            TextureDesc descriptor = renderGraph.GetTextureDesc(source);
            descriptor.name = "Underwater Color";
            descriptor.clearBuffer = false;
            TextureHandle destination = renderGraph.CreateTexture(descriptor);

            RenderTextureDescriptor target = frameData.Get<UniversalCameraData>().cameraTargetDescriptor;
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Underwater Blur and Haze", out var data))
            {
                data.source = source;
                data.depth = resources.cameraDepthTexture;
                data.material = material;
                data.properties = properties;
                data.effect = effectParameters;
                data.pixelSize = new Vector4(1f / target.width, 1f / target.height, 0f, 0f);
                data.oceanColor = oceanColor;
                builder.UseTexture(data.source);
                builder.UseTexture(data.depth);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                builder.SetRenderFunc(static (PassData passData, RasterGraphContext context) =>
                {
                    var block = passData.properties;
                    block.SetTexture(BlitTexture, passData.source);
                    block.SetTexture(DepthTexture, passData.depth);
                    block.SetVector(BlitScaleBias, new Vector4(1f, 1f, 0f, 0f));
                    block.SetVector(UnderwaterParams, passData.effect);
                    block.SetVector(PixelSize, passData.pixelSize);
                    block.SetColor(OceanColor, passData.oceanColor);
                    context.cmd.DrawProcedural(Matrix4x4.identity, passData.material, 0,
                        MeshTopology.Triangles, 3, 1, block);
                });
            }
            resources.cameraColor = destination;
        }
    }
}
