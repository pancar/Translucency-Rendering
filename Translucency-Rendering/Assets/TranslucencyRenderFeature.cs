using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;


public class TranslucencyRenderFeature : ScriptableRendererFeature
{
    public static RendererListDesc InitRendererListParams(ContextContainer frameData,LayerMask layerMask, ShaderTagId _shaderTagId)
    {
        UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();

        var sortFlags = cameraData.defaultOpaqueSortFlags;
        RenderQueueRange
            renderQueueRange =
                RenderQueueRange.opaque;
        

        var param = new RendererListDesc(_shaderTagId, universalRenderingData.cullResults, cameraData.camera);

        param.sortingCriteria = sortFlags;
        param.renderQueueRange = renderQueueRange;
        param.layerMask = layerMask;

        param.rendererConfiguration = PerObjectData.None;
        param.excludeObjectMotionVectors = true;
        param.overrideShaderPassIndex = 0;

        return param;
    }

    public static RendererListDesc InitRendererListDesc(ContextContainer frameData, Material mat,
        LayerMask layerMask, ShaderTagId _shaderTagId)
    {
        UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();

        var sortFlags = cameraData.defaultOpaqueSortFlags;
        RenderQueueRange
            renderQueueRange =
                RenderQueueRange.opaque;


        var param = new RendererListDesc(_shaderTagId, universalRenderingData.cullResults, cameraData.camera);

        param.sortingCriteria = sortFlags;
        param.renderQueueRange = renderQueueRange;
        param.layerMask = layerMask;

        param.rendererConfiguration = PerObjectData.None;
        param.excludeObjectMotionVectors = true;
        param.overrideShaderPassIndex = 0;
        param.overrideMaterial = mat;

        return param;
    }

    public LayerMask _TranslucentLayerMask;
    public LayerMask _OccluderLayerMask;


    [SerializeField] Material _material_Front;
    [SerializeField] Material _material_Back;
    [SerializeField] Material _material_Thickness;
    [SerializeField] private Material blurMaterial;
    [SerializeField] [Range(1, 25)] private int blurScale = 5;

    RendererListFrontPass _frontPosPass;
    RendererListBackPass _backListPass;
    ThicknessPass _thicknessPass;
    ThicknessBlurPass _thicknessBlurPass;
    OccluderPass _occluderPass;


    /// <inheritdoc/>
    public override void Create()
    {
        _frontPosPass = new RendererListFrontPass();
        _frontPosPass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        _backListPass = new RendererListBackPass();
        _backListPass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        _thicknessPass = new ThicknessPass();
        _thicknessPass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        _thicknessBlurPass = new ThicknessBlurPass();
        _thicknessBlurPass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        _occluderPass = new OccluderPass();
        _occluderPass.renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _frontPosPass.Setup(_material_Front, _TranslucentLayerMask);
        _backListPass.Setup(_material_Back, _TranslucentLayerMask);
        _thicknessPass.Setup(_material_Thickness);
        _thicknessBlurPass.Setup(blurScale, blurMaterial);
        _occluderPass.Setup(_OccluderLayerMask);

        renderer.EnqueuePass(_occluderPass);
        renderer.EnqueuePass(_frontPosPass);
        renderer.EnqueuePass(_backListPass);
        renderer.EnqueuePass(_thicknessPass);
        renderer.EnqueuePass(_thicknessBlurPass);
    }
}