Shader "Depth/Unlit"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.meta.xr.depthapi.urp/Shaders/EnvironmentOcclusionURP.hlsl"

            struct Attributes
            {
                float4 vertex : POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 positionNDC : TEXCOORD1;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                // Very important to support stereo rendering
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);

                Varyings output;
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                // Stereo rendering support
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.vertex.xyz);

                // Calculate normalized display coordinates
                float4 ndc = output.positionCS * 0.5f;
                // pass them to frag stage
                output.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
                output.positionNDC.zw = output.positionCS.zw;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                // Stereo rendering support
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 finalColor = _BaseColor;

                // Calculate screen space UV
                float2 uv = input.positionNDC.xy / input.positionNDC.w;
                // Calculate occlusion value where
                // 1 - not occluded
                // 0 - fully occluded
                // 0-1 - gradient if soft occlusions enabled
                float occlusionValue = CalculateEnvironmentDepthOcclusion(uv, input.positionCS.z);

                // Reject non visible fragments so they don't render to depth
                if (occlusionValue < 0.05)
                {
                    discard;
                }

                // multiply color and alpha
                finalColor *= occlusionValue;

                return finalColor ;
            }
            ENDHLSL
        }
    }
}
