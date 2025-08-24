Shader "Custom/DepthReprojectMeters"
{
    Properties
    {
        _Alpha         ("Alpha", Range(0,1)) = 1.0
        _UsePreprocessed ("Use Preprocessed Depth", Float) = 1.0
        _UseStereo     ("Use Stereo Eye Slice", Float) = 1.0
        _EyeIndex      ("Fallback Eye Index", Float) = 0.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO
            #include "UnityCG.cginc"

            // Globals set by EnvironmentDepthManager each frame:
            // _EnvironmentDepthReprojectionMatrices[2], _EnvironmentDepthZBufferParams,
            // _EnvironmentDepthTexture, _PreprocessedEnvironmentDepthTexture
            UNITY_DECLARE_TEX2DARRAY(_EnvironmentDepthTexture);
            UNITY_DECLARE_TEX2DARRAY(_PreprocessedEnvironmentDepthTexture);
            uniform float4x4 _EnvironmentDepthReprojectionMatrices[2];
            uniform float4   _EnvironmentDepthZBufferParams;

            float  _Alpha;
            float  _UsePreprocessed;
            float  _UseStereo;
            float  _EyeIndex;

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos      = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // Choose eye: use stereo index if active, else fallback property
                int eye = ( _UseStereo > 0.5 ) ? unity_StereoEyeIndex : (int)_EyeIndex;

                // World -> depth camera clip space (same math occlusion uses)
                float4 clip = mul(_EnvironmentDepthReprojectionMatrices[eye], float4(i.worldPos, 1.0));
                if (clip.w <= 0) return 0; // behind depth camera

                float2 uv = clip.xy / clip.w * 0.5 + 0.5;
                if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) return 0;

                // Sample raw/preprocessed depth buffer (red channel)
                float depthBuf;
                if (_UsePreprocessed > 0.5)
                    depthBuf = UNITY_SAMPLE_TEX2DARRAY(_PreprocessedEnvironmentDepthTexture, float3(uv, eye)).r;
                else
                    depthBuf = UNITY_SAMPLE_TEX2DARRAY(_EnvironmentDepthTexture,            float3(uv, eye)).r;

                // Convert depth buffer value to **linear meters**:
                // meters = 1 / (z_ndc + B) * A, where (A,B) = _EnvironmentDepthZBufferParams.xy
                float z_ndc  = depthBuf * 2.0 - 1.0;
                float meters = 1.0 / (z_ndc + _EnvironmentDepthZBufferParams.y) * _EnvironmentDepthZBufferParams.x;

                // Write raw meters to RED (no normalization). Requires a float RT to avoid clamping.
                return float4(meters, 0, 0, _Alpha);
            }
            ENDCG
        }
    }
    FallBack Off
}
