using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

class OccluderPass : ScriptableRenderPass
{
    private LayerMask _layerMask;
    private ShaderTagId _shaderTagId = new ShaderTagId("UniversalForward");
    public static int occluderTextureID = Shader.PropertyToID("_GlobalOccluderTexture");

    public void Setup(LayerMask layerMask)
    {
        _layerMask = layerMask;
    }

    private class PassData
    {
        public RendererListHandle rendererListHandle;
        public TextureHandle occluderTH;
    }

    // public class CustomDataOccluder : ContextItem
    // {
    //     public TextureHandle newOccluderTH;
    //
    //     public override void Reset()
    //     {
    //         newOccluderTH = TextureHandle.nullHandle;
    //     }
    // }
    static void ExecutePass(PassData data, RasterGraphContext context)
    {
        context.cmd.ClearRenderTarget(RTClearFlags.ColorDepth, Color.clear, 1, 0);
        context.cmd.DrawRendererList(data.rendererListHandle);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        string passName = "Occluder Pass";

        using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
        {
            // UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            passData.rendererListHandle =
                renderGraph.CreateRendererList(TranslucencyRenderFeature.InitRendererListParams(frameData, _layerMask,
                    _shaderTagId));

            RenderTextureDescriptor textureProperties =
                new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.ARGBHalf, 0);
            RenderTextureDescriptor texturePropertiesDepth =
                new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Depth, 16);


            TextureHandle textureOccluderTH =
                UniversalRenderer.CreateRenderGraphTexture(renderGraph, textureProperties,
                    "Occluder Texture", false);
            TextureHandle textureBackDepth =
                UniversalRenderer.CreateRenderGraphTexture(renderGraph, texturePropertiesDepth,
                    "Occluder Depth Texture", false);

            passData.occluderTH = textureOccluderTH;
            builder.AllowGlobalStateModification(true);
            builder.SetRenderAttachment(passData.occluderTH, 0, AccessFlags.Write);
            builder.SetRenderAttachmentDepth(textureBackDepth);

            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.SetGlobalTextureAfterPass(textureOccluderTH, occluderTextureID);
            builder.UseRendererList(passData.rendererListHandle);

            // CustomDataOccluder customData = frameData.Create<CustomDataOccluder>();
            // customData.newOccluderTH = textureOccluderTH;


            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                ExecutePass(data, context));
        }
    }
}