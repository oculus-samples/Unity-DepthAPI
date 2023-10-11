Shader "Meta/Depth/BiRP/ExampleUnlitShader"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        // 0. It's important to have One OneMinusSrcAlpha so it blends properly against transparent background (passthrough)
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            // 1. Keywords are used to enable different occlusions
            #pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // 2. Include the file with utility functions
            #include "Packages/com.meta.xr.depthapi/Runtime/BiRP/EnvironmentOcclusionBiRP.cginc"

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float4 positionNDC : TEXCOORD1;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                // 3. Fragment shader needs stereo information to understand what eye
                // is currently rendered to get depth from the correct texture
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                output.positionCS = UnityObjectToClipPos(input.vertex);;
                output.uv = input.uv;

                // 4. Screen position is required in normalized display coordinates form to query the depth
                float4 ndc = output.positionCS * 0.5f;
                output.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
                output.positionNDC.zw = output.positionCS.zw;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                // 5. Passes stereo information to frag shader
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                // 6. Initializes global stereo constant for the frag shader
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 finalColor = _BaseColor;

                // 7. Calculate UV
                float2 uv = input.positionNDC.xy / input.positionNDC.w;

                // 8. Calculate occlusion value:
                    // 0 - completely occluded
                    // 1 - completely visible
                    // 0-1 - soft occlusion if enabled
                float occlusionValue = CalculateEnvironmentDepthOcclusion(uv, input.positionCS.z);
                if (occlusionValue < 0.01) {
                  discard;
                }

                // 9. premultiply alpha and color with occlusions
                finalColor *= occlusionValue;

                return finalColor;
            }
            ENDCG
        }
    }
}
