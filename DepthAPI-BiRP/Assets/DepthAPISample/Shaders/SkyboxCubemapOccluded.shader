// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Skybox/CubemapOccluded" {
Properties {
    _Tint ("Tint Color", Color) = (.5, .5, .5, .5)
    [Gamma] _Exposure ("Exposure", Range(0, 8)) = 1.0
    _Rotation ("Rotation", Range(0, 360)) = 0
    [NoScaleOffset] _Tex ("Cubemap   (HDR)", Cube) = "grey" {}
}

SubShader {
    Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
    Cull Off ZWrite Off

    Pass {

        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 3.0

        #pragma multi_compile_instancing
        #pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION
        
        #include "UnityCG.cginc"
        #include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/BiRP/EnvironmentOcclusionBiRP.cginc"
        
        samplerCUBE _Tex;
        half4 _Tex_HDR;
        half4 _Tint;
        half _Exposure;
        float _Rotation;
        float _EnvironmentDepthBias;
        
        float3 RotateAroundYInDegrees (float3 vertex, float degrees)
        {
            float alpha = degrees * UNITY_PI / 180.0;
            float sina, cosa;
            sincos(alpha, sina, cosa);
            float2x2 m = float2x2(cosa, -sina, sina, cosa);
            return float3(mul(m, vertex.xz), vertex.y).xzy;
        }

        struct vertexInput {
            float4 vertex : POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct vertexOutput {
            float4 vertex : SV_POSITION;
            float3 texcoord : TEXCOORD0;
            META_DEPTH_VERTEX_OUTPUT(1)
            UNITY_VERTEX_OUTPUT_STEREO
        };

        vertexOutput vert (vertexInput v)
        {
            vertexOutput output;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            float3 rotated = RotateAroundYInDegrees(v.vertex, _Rotation);
            output.vertex = UnityObjectToClipPos(rotated);
            output.texcoord = v.vertex.xyz;
            META_DEPTH_INITIALIZE_VERTEX_OUTPUT(output, v.vertex);
            return output;
        }

        fixed4 frag (vertexOutput i) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

            float occlusionValue = META_DEPTH_GET_OCCLUSION_VALUE_WORLDPOS(i.posWorld, _EnvironmentDepthBias);
            if (occlusionValue < 0.01) { //early exit
                discard; 
            } 
            
            half4 tex = texCUBE (_Tex, i.texcoord);
            half3 c = DecodeHDR (tex, _Tex_HDR);
            c = c * _Tint.rgb * unity_ColorSpaceDouble.rgb;
            c *= _Exposure;
            c *= occlusionValue;
            return half4(c, occlusionValue);
        }
        ENDCG
    }
}


Fallback Off

}
