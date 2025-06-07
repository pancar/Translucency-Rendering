using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

class ThicknessPass : ScriptableRenderPass
{
    static Vector4 _scaleBias = new Vector4(1f, 1f, 0f, 0f);
    Material _material;
    // int _thicknessTextureID = Shader.PropertyToID("_GlobalThicknessTexture");

    class PassData
    {
        public TextureHandle destination;
        public Material material;
    }

    public void Setup(Material material)
    {
        _material = material;
    }

    public class CustomDataThickness : ContextItem
    {
        public TextureHandle newThicknessTH;

        public override void Reset()
        {
            newThicknessTH = TextureHandle.nullHandle;
        }
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
    {
        string passName = "RenderList Render Thickness Pass";
        using (var builder =
               renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
        {
            var resourceData = frameContext.Get<UniversalResourceData>();
            //builder.AllowGlobalStateModification(true);
            // Fetch the yellow texture from the frame data and set it as the render target
            //   var customDataFront = frameContext.Get<RendererListFrontPass.CustomDataFront>();
            // var customTextureFront = customDataFront.newFrontPosTH;
            builder.UseGlobalTexture(RendererListFrontPass.frontPosTextureID);
            builder.UseGlobalTexture(RendererListBackPass.backPosTextureID);
            // builder.UseGlobalTexture(RendererListFrontPass.frontDepthTextureID);
            // builder.UseGlobalTexture(RendererListBackPass.backDepthTextureID);

            RenderTextureDescriptor textureProperties =
                new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.ARGBHalf, 0);
            TextureHandle thicknessTextureHandle =
                UniversalRenderer.CreateRenderGraphTexture(renderGraph, textureProperties,
                    "targeTextureHandle", false);
            passData.destination = thicknessTextureHandle;
            passData.material = _material;


            builder.AllowPassCulling(false);
            builder.SetRenderAttachment(passData.destination, 0);
            //builder.SetGlobalTextureAfterPass(thicknessTextureHandle, _thicknessTextureID);
            CustomDataThickness customData = frameContext.Create<CustomDataThickness>();
            customData.newThicknessTH = thicknessTextureHandle;
            // Set the main color texture for the camera as the destination texture
            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                ExecutePass(data, context));
        }
    }

    static void ExecutePass(PassData data, RasterGraphContext context)
    {
        Blitter.BlitTexture(context.cmd, data.destination, _scaleBias, data.material, 0);
    }
}