using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

class RendererListBackPass : ScriptableRenderPass
{
    Material _mat;
    private LayerMask _layerMask;
    private ShaderTagId _shaderTagId = new ShaderTagId("UniversalForward");

    public static int backPosTextureID = Shader.PropertyToID("_GlobalBackPosTexture");
    // public static int backDepthTextureID = Shader.PropertyToID("_GlobalBackDepthTexture");

    public void Setup(
        Material mat, LayerMask layerMask)
    {
        _mat = mat;
        _layerMask = layerMask;
    }

    private class PassData
    {
        public RendererListHandle rendererListHandle;
        public TextureHandle frontPosTH;
    }

    static void ExecutePass(PassData data, RasterGraphContext context)
    {
        context.cmd.ClearRenderTarget(RTClearFlags.ColorDepth, Color.clear, 1, 0);
        context.cmd.DrawRendererList(data.rendererListHandle);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        string passName = "RenderList Render Back Pass";


        using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
        {
            // UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();


            passData.rendererListHandle =
                renderGraph.CreateRendererList(TranslucencyRenderFeature.InitRendererListDesc(frameData, _mat,
                    _layerMask,
                    _shaderTagId));

            RenderTextureDescriptor textureProperties =
                new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.ARGBHalf, 0);
            RenderTextureDescriptor texturePropertiesDepth =
                new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Depth, 16);


            TextureHandle textureBackPos =
                UniversalRenderer.CreateRenderGraphTexture(renderGraph, textureProperties,
                    "Back Position", false);
            TextureHandle textureBackDepth =
                UniversalRenderer.CreateRenderGraphTexture(renderGraph, texturePropertiesDepth,
                    "Back Depth", false);


            builder.AllowGlobalStateModification(true);
            builder.SetRenderAttachment(textureBackPos, 0, AccessFlags.Write);
             builder.SetRenderAttachmentDepth(textureBackDepth);

            builder.SetGlobalTextureAfterPass(textureBackPos, backPosTextureID);
            // builder.SetGlobalTextureAfterPass(textureBackDepth, backDepthTextureID);

            builder.UseRendererList(passData.rendererListHandle);
            // builder.AllowGlobalStateModification(true);

            builder.AllowPassCulling(false);

            // Assign the ExecutePass function to the render pass delegate, which will be called by the render graph when executing the pass
            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                ExecutePass(data, context));

            //  builder.SetRenderFunc<PassData>(ExecutePass);
        }
    }
}