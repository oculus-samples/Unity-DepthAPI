Shader "Hidden/DepthReprojectMeters_Blit"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Your existing globals (set by your manager):
            UNITY_DECLARE_TEX2DARRAY(_EnvironmentDepthTexture);
            UNITY_DECLARE_TEX2DARRAY(_PreprocessedEnvironmentDepthTexture);
            float4x4 _EnvironmentDepthReprojectionMatrices[2];
            float4x4 _EnvironmentDepthInvProjectionMatrices[2];
            float4   _EnvironmentDepthZBufferParams;
            float    _UsePreprocessed;
            float    _UseStereo;   // set to 0 for this offscreen pass, unless you really want instanced stereo
            float    _EyeIndex;    // choose 0 or 1 manually
            float    _Alpha;

            // Plane description (world-space):
            float3 _PlaneCenterWS;
            float3 _PlaneRightHalfWS;
            float3 _PlaneUpHalfWS;

            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (uint id : SV_VertexID)
            {
                v2f o;
                float2 pos = float2((id << 1) & 2, id & 2); // (0,0), (0,2), (2,0)
                o.pos = float4(pos * 2.0 - 1.0, 0.0, 1.0);
                o.uv  = pos ; // 0..1
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {

                // For this offscreen blit, usually set _UseStereo=0 and choose _EyeIndex manually.
                int eye = (_UseStereo > 0.5) ? unity_StereoEyeIndex : (int)_EyeIndex;

                // Flip vertically
                float2 uv = i.uv;
                uv.y =  1.0-uv.y;  // <-- Flip V

                // Map uv (0..1) -> (-1..1)
                float2 uvN = uv *2.0 - 1.0;

                // Reconstruct world position on the moving/rotating plane
                float3 worldPos = _PlaneCenterWS
                                + uvN.x * _PlaneRightHalfWS
                                + uvN.y * _PlaneUpHalfWS;

                // World -> depth camera clip space
                float4 clip = mul(_EnvironmentDepthReprojectionMatrices[eye], float4(worldPos, 1.0));
                if (clip.w <= 0) return 0;

                // Clip -> depth UV
                float2 duv = clip.xy / clip.w * 0.5 + 0.5;
                if (duv.x < 0 || duv.x > 1 || duv.y < 0 || duv.y > 1) return 0;

                // Sample depth (R)
                float d = (_UsePreprocessed > 0.5)
                    ? UNITY_SAMPLE_TEX2DARRAY(_PreprocessedEnvironmentDepthTexture, float3(duv, eye)).r
                    : UNITY_SAMPLE_TEX2DARRAY(_EnvironmentDepthTexture,            float3(duv, eye)).r;

                // Buffer -> meters
                float z_ndc  = d * 2.0 - 1.0;
                float meters = 1.0 / (z_ndc + _EnvironmentDepthZBufferParams.y) * _EnvironmentDepthZBufferParams.x;
                
                // Screen -> NDC
                float2 ndcXY = uv * 2.0 - 1.0;

                // Unproject to CAMERA (view) space: multiply by inverse projection, then divide by w
                float4 hCam = mul(_EnvironmentDepthInvProjectionMatrices[eye], float4(ndcXY, z_ndc, 1.0));
                float3 camPos = hCam.xyz / hCam.w;

                // NOTE: With Unity’s view-convention, forward is -Z. If you prefer +Z forward, uncomment:
                // camPos.z = -camPos.z;

                // Finally: pack meters in R, and camera-space XYZ in GBA
                return float4(meters, camPos);
                //return float4(meters, 0, 0, _Alpha);
                //return float4(uv, 0, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
