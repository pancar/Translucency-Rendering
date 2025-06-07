using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

class RendererListFrontPass : ScriptableRenderPass
{
    Material _mat;
    private LayerMask _layerMask;
    private ShaderTagId _shaderTagId = new ShaderTagId("PositionFrontWS");

    public static int frontPosTextureID = Shader.PropertyToID("_GlobalFrontPosTexture");
    // public static int frontDepthTextureID = Shader.PropertyToID("_GlobalFrontDepthTexture");

    public void Setup(
        Material mat, LayerMask layerMask)
    {
        _mat = mat;
        _layerMask = layerMask;
    }


    private class PassData
    {
        public RendererListHandle rendererListHandle;
    }


    static void ExecutePass(PassData data, RasterGraphContext context)
    {
        context.cmd.ClearRenderTarget(RTClearFlags.ColorDepth, Color.clear, 1, 0);
        context.cmd.DrawRendererList(data.rendererListHandle);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        string passName = "RenderList Render Front Pass";


        using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
        {
            // UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            _shaderTagId = new ShaderTagId("UniversalForward");
            passData.rendererListHandle =
                renderGraph.CreateRendererList(TranslucencyRenderFeature.InitRendererListDesc(frameData, _mat,
                    _layerMask, _shaderTagId));

            RenderTextureDescriptor textureProperties =
                new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.ARGBHalf, 0);
            RenderTextureDescriptor texturePropertiesDepth =
                new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Depth, 16);

            TextureHandle textureFrontPos =
                UniversalRenderer.CreateRenderGraphTexture(renderGraph, textureProperties,
                    "Front Position", false);

            TextureHandle textureFrontDepth =
                UniversalRenderer.CreateRenderGraphTexture(renderGraph, texturePropertiesDepth,
                    "Front Position Depth", false);

            builder.AllowGlobalStateModification(true);
            builder.UseRendererList(passData.rendererListHandle);

            builder.SetRenderAttachment(textureFrontPos, 0, AccessFlags.ReadWrite);
            builder.SetRenderAttachmentDepth(textureFrontDepth, AccessFlags.Write);

            builder.SetGlobalTextureAfterPass(textureFrontPos, frontPosTextureID);
            // builder.SetGlobalTextureAfterPass(textureFrontDepth, frontDepthTextureID);

            builder.AllowPassCulling(false);

            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                ExecutePass(data, context));
        }
    }
}