using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


class ThicknessBlurPass : ScriptableRenderPass
{
    private RTHandle _sourceRTHandle;
    private RTHandle _halfRTHandle;
    private RTHandle _quarterRTHandle;
    private RTHandle _eighthRTHandle;

    private TextureHandle _sourceTextureHandle;
    private TextureHandle _halfTextureHandle;
    private TextureHandle _quarterTextureHandle;
    private TextureHandle _eighthTextureHandle;

    private RenderTextureDescriptor _sourceTextureDescriptor;
    private RenderTextureDescriptor _halfTextureDescriptor;
    private RenderTextureDescriptor _quarterTextureDescriptor;
    private RenderTextureDescriptor _eighthTextureDescriptor;

    private readonly int _blurScaleID = Shader.PropertyToID("_BlurScale");
    private int _blurredTextureID = Shader.PropertyToID("BlurredThicknessTex");
    private float _blurScale;
    private Material _material;
    private bool _toCameraColor;

    public void Setup(float blurScale, Material blurMaterial)
    {
        _blurScale = blurScale;
        _material = blurMaterial;
    }

    private void Init(RenderGraph renderGraph, RenderTextureDescriptor cameraTextureDescriptor)
    {
        
        _sourceTextureDescriptor = new RenderTextureDescriptor(Screen.width,
            Screen.height, RenderTextureFormat.Default, 0);
        _halfTextureDescriptor = new RenderTextureDescriptor(Screen.width / 2,
            Screen.height / 2, RenderTextureFormat.Default, 0);
        _quarterTextureDescriptor = new RenderTextureDescriptor(Screen.width / 4,
            Screen.height / 4, RenderTextureFormat.Default, 0);
        _eighthTextureDescriptor = new RenderTextureDescriptor(Screen.width / 8,
            Screen.height / 8, RenderTextureFormat.Default, 0);
        

        _sourceTextureDescriptor.width = cameraTextureDescriptor.width;
        _sourceTextureDescriptor.height = cameraTextureDescriptor.height;
        _sourceTextureDescriptor.dimension = cameraTextureDescriptor.dimension;
        _sourceTextureDescriptor.graphicsFormat = cameraTextureDescriptor.graphicsFormat;

        _halfTextureDescriptor.width = cameraTextureDescriptor.width / 2;
        _halfTextureDescriptor.height = cameraTextureDescriptor.height / 2;
        _halfTextureDescriptor.dimension = cameraTextureDescriptor.dimension;
        _halfTextureDescriptor.graphicsFormat = cameraTextureDescriptor.graphicsFormat;

        _quarterTextureDescriptor.width = cameraTextureDescriptor.width / 4;
        _quarterTextureDescriptor.height = cameraTextureDescriptor.height / 4;
        _quarterTextureDescriptor.dimension = cameraTextureDescriptor.dimension;
        _quarterTextureDescriptor.graphicsFormat = cameraTextureDescriptor.graphicsFormat;
        
        _eighthTextureDescriptor.width = cameraTextureDescriptor.width / 8;
        _eighthTextureDescriptor.height = cameraTextureDescriptor.height / 8;
        _eighthTextureDescriptor.dimension = cameraTextureDescriptor.dimension;
        _eighthTextureDescriptor.graphicsFormat = cameraTextureDescriptor.graphicsFormat;

        RenderingUtils.ReAllocateHandleIfNeeded(ref _sourceRTHandle, _sourceTextureDescriptor, FilterMode.Bilinear,
            TextureWrapMode.Clamp, name: "sourceTexture");
        RenderingUtils.ReAllocateHandleIfNeeded(ref _halfRTHandle, _halfTextureDescriptor, FilterMode.Bilinear,
            TextureWrapMode.Clamp, name: "halfTexture");
        RenderingUtils.ReAllocateHandleIfNeeded(ref _quarterRTHandle, _quarterTextureDescriptor,
            FilterMode.Bilinear,
            TextureWrapMode.Clamp, name: "quarterTexture");
        RenderingUtils.ReAllocateHandleIfNeeded(ref _eighthRTHandle, _eighthTextureDescriptor,
            FilterMode.Bilinear,
            TextureWrapMode.Clamp, name: "eighthTexture");

        _sourceTextureHandle = renderGraph.ImportTexture(_sourceRTHandle);
        _halfTextureHandle = renderGraph.ImportTexture(_halfRTHandle);
        _quarterTextureHandle = renderGraph.ImportTexture(_quarterRTHandle);
        _eighthTextureHandle = renderGraph.ImportTexture(_eighthRTHandle);
    }

    class PassData
    {
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        RecordBlurredColor(renderGraph, frameData, _blurScale, _material);
    }

    private void RecordBlurredColor(RenderGraph renderGraph, ContextContainer frameData, float blurScale,
        Material blitMat)
    {
        
        var customDataThickness = frameData.Get<ThicknessPass.CustomDataThickness>();
        var customTextureThickness = customDataThickness.newThicknessTH;
        
        if (!_sourceTextureHandle.IsValid())
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var descriptor = cameraData.cameraTargetDescriptor;
            descriptor.msaaSamples = 1;
            descriptor.graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;
            descriptor.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None;
            Init(renderGraph, descriptor);
        }

        blitMat.SetFloat(_blurScaleID, blurScale);

        var resourceData = frameData.Get<UniversalResourceData>();
        if (resourceData.isActiveTargetBackBuffer)
        {
            Debug.LogError(
                $"Skipping render pass. BlurRendererFeature requires an intermediate ColorTexture, we can't use the BackBuffer as a texture input.");
            return;
        }

        var source = customTextureThickness;

        RenderGraphUtils.BlitMaterialParameters downHalfParameters =
            new(source, _halfTextureHandle, blitMat, 0);
        renderGraph.AddBlitPass(downHalfParameters, passName: "DownHalfPass");
        
        RenderGraphUtils.BlitMaterialParameters downQuarterParameters =
            new(_halfTextureHandle, _quarterTextureHandle, blitMat, 0);
        renderGraph.AddBlitPass(downQuarterParameters, passName: "DownQuarterPass");
        
        RenderGraphUtils.BlitMaterialParameters downEighthParameters =
            new(_quarterTextureHandle, _eighthTextureHandle, blitMat, 0);
        renderGraph.AddBlitPass(downEighthParameters, passName: "DownEighthPass");
        
        RenderGraphUtils.BlitMaterialParameters upQuarterParameters =
            new(_eighthTextureHandle, _quarterTextureHandle, blitMat, 1);
        renderGraph.AddBlitPass(upQuarterParameters, passName: "UpQuarterPass");

        RenderGraphUtils.BlitMaterialParameters upHalfParameters =
            new(_quarterTextureHandle, _halfTextureHandle, blitMat, 1);
        renderGraph.AddBlitPass(upHalfParameters, passName: "UpHalfPass");
        RenderGraphUtils.BlitMaterialParameters upSourceParameters =
            new(_halfTextureHandle, _sourceTextureHandle, blitMat, 1);
        renderGraph.AddBlitPass(upSourceParameters, passName: "UpSourcePass");

        using (var builder = renderGraph.AddRasterRenderPass<PassData>("ThicknessBlur", out var passData))
        {
            builder.AllowGlobalStateModification(true);
            builder.SetGlobalTextureAfterPass(_sourceTextureHandle, _blurredTextureID);
            builder.SetRenderFunc((PassData data, RasterGraphContext context) => { });
        }
    }
}