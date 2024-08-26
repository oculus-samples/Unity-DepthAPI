Shader "Meta/EnvironmentDepth/BiRP/OcclusionCutoutBiRP"
{
    Properties
    {
        _MainTex("Cutout Texture", 2D) = "white"
        _EnvironmentDepthBias ("Environment Depth Bias", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" }
        LOD 100

        Blend Zero SrcAlpha
        ZTest LEqual

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION

            #include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/BiRP/EnvironmentOcclusionBiRP.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv :TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv :TEXCOORD0;
                META_DEPTH_VERTEX_OUTPUT(1)

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _EnvironmentDepthBias;

            v2f vert (appdata v) {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.vertex = UnityObjectToClipPos(v.vertex);

                META_DEPTH_INITIALIZE_VERTEX_OUTPUT(o, v.vertex);

                return o;
            }

            half4 frag (v2f i) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i)
                fixed4 cutoutMask = tex2D (_MainTex, i.uv);
#if defined(HARD_OCCLUSION) || defined(SOFT_OCCLUSION)
                float occlusionValue = META_DEPTH_GET_OCCLUSION_VALUE(i, _EnvironmentDepthBias);
#else
                float occlusionValue = 1.0f;
#endif
                if(cutoutMask.a == 0)
                    occlusionValue = 1.0f;
                return half4(0, 0, 0, occlusionValue);
            }
            ENDCG
        }
    }
}
