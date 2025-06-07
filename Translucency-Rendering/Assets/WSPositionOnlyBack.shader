// This shader fills the mesh shape with a color predefined in the code.
Shader "Hidden/PositionWS_Back"
{

    Properties {}


    SubShader
    {

        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline"
        }

        Pass
        {
            Name "FrontPass"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            // -------------------------------------
            // Render State Commands
            ZWrite On
            //ColorMask R
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
            };


            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                return OUT;
            }

            // The fragment shader definition.            
            half4 frag(Varyings IN) : SV_Target
            {
                half4 customColor;
                customColor = half4(IN.positionWS.xyz, 1);
                return customColor;
            }
            ENDHLSL
        }


    }
}