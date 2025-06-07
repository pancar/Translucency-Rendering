Shader "ThicknessCalculate"
{
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"
        }
        ZWrite Off Cull Off

        Pass
        {
            Name "ThicknessCalculatePass"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            TEXTURE2D_X(_GlobalFrontPosTexture);
            TEXTURE2D_X(_GlobalBackPosTexture);
            SAMPLER(sampler_GlobalFrontPosTexture);
            SAMPLER(sampler_GlobalBackPosTexture);
            TEXTURE2D_X(_GlobalFrontDepthTexture);
            TEXTURE2D_X(_GlobalBackDepthTexture);
            SAMPLER(sampler_GlobalFrontDepthTexture);
            SAMPLER(sampler_GlobalBackDepthTexture);

            TEXTURE2D_X(_GlobalOccluderTexture);
            SAMPLER(sampler_GlobalOccluderTexture);


            float3 NormalizeVector01(float3 v)
            {
                float minVal = min(v.x, min(v.y, v.z));
                float maxVal = max(v.x, max(v.y, v.z));
                float range = maxVal - minVal;

                // range 0 ise bölme hatasını önle
                return range > 0 ? (v - minVal) / range : float3(0.0, 0.0, 0.0);
            }

            // Out frag function takes as input a struct that contains the screen space coordinate we are going to use to sample our texture. It also writes to SV_Target0, this has to match the index set in the UseTextureFragment(sourceTexture, 0, …) we defined in our render pass script.   
            float4 Frag(Varyings input) : SV_Target0
            {
                // this is needed so we account XR platform differences in how they handle texture arrays
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // sample the texture using the SAMPLE_TEXTURE2D_X_LOD
                float2 uv = input.texcoord.xy;
                half4 front = SAMPLE_TEXTURE2D_X_LOD(_GlobalFrontPosTexture, sampler_GlobalFrontPosTexture, uv,
                                                     _BlitMipLevel);
                half4 back = SAMPLE_TEXTURE2D_X_LOD(_GlobalBackPosTexture, sampler_GlobalBackPosTexture, uv,
                                                    _BlitMipLevel);

                float4 color = float4(0, 0, 0, 0);
                half4 frontDepth = SAMPLE_TEXTURE2D_X_LOD(_GlobalFrontDepthTexture, sampler_GlobalFrontDepthTexture, uv,
                    _BlitMipLevel);
                half4 backDepth = SAMPLE_TEXTURE2D_X_LOD(_GlobalBackDepthTexture, sampler_GlobalBackDepthTexture, uv,
                    _BlitMipLevel);


                float linearA = LinearEyeDepth(frontDepth.r, _ZBufferParams);
                float linearB = LinearEyeDepth(backDepth.r, _ZBufferParams);

                bool validA = (linearA < 0.999) && (front.a > 0.01);
                bool validB = (linearB < 0.999) && (back.a > 0.01);


                float distance = length(back - front);
                //float normalized = saturate((distance - minDist) / (maxDist - minDist));
                float normalized = saturate((distance - 0) / (2 - 0));
                //  normalized = smoothstep(0,0.2,normalized);
                // color =  back-front;
                color.r = 1 - normalized;

                half4 occluderCol = SAMPLE_TEXTURE2D_X_LOD(_GlobalOccluderTexture, sampler_GlobalOccluderTexture, uv,
                    _BlitMipLevel);
                color.a = color.r;// * (1 - occluderCol.a);
                color.rgb = lerp(float3(1, 1, 1), occluderCol.rgb, occluderCol.a);
                // lerp(color.rgb,occluderCol.rgb,occluderCol.a);
                //return occluderCol;
                // Modify the sampled color
                return color;
            }
            ENDHLSL
        }
    }
}