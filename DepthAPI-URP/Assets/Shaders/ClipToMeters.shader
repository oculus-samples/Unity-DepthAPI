// In your ClipToMetres.shader file

Shader "Hidden/ClipToMetres"
{
    Properties
    {
        // This property will be set by the C# script.
        _SliceIndex ("Slice Index", Int) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5 // Required for texture arrays

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Define the source texture as a Texture2D Array
            TEXTURE2D_ARRAY(_MainTex);
            SAMPLER(sampler_MainTex);

            // Define a buffer to receive the slice index from the material
            CBUFFER_START(UnityPerMaterial)
                int _SliceIndex;
            CBUFFER_END

            struct Attributes {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            Varyings vert(Attributes input) {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings i) : SV_Target
            {
                // Sample the depth from the correct slice of the texture array
                float raw_depth = SAMPLE_TEXTURE2D_ARRAY(_MainTex, sampler_MainTex, i.uv, _SliceIndex).r;

                // The rest is standard perspective-to-linear depth conversion
                float linear_eye_depth = LinearEyeDepth(raw_depth, _ZBufferParams);
                return float4(linear_eye_depth, linear_eye_depth, linear_eye_depth, 1.0);
            }
            ENDHLSL
        }
    }
}