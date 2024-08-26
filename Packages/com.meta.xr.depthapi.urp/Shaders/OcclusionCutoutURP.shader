/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

Shader "Meta/EnvironmentDepth/URP/OcclusionCutoutURP"
{
    Properties
    {
        _MainTex("Cutout Texture", 2D) = "white"
        _EnvironmentDepthBias ("Environment Depth Bias", Float) = 0.0
    }

    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.universal": "14.0"
        }
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" }
        LOD 100

        Blend Zero SrcAlpha
        ZTest LEqual

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/URP/EnvironmentOcclusionURP.hlsl"
            #pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION

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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            float _EnvironmentDepthBias;

            v2f vert (appdata v) {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.vertex = TransformObjectToHClip(v.vertex.xyz);

                META_DEPTH_INITIALIZE_VERTEX_OUTPUT(o, v.vertex);

                return o;
            }

            half4 frag (v2f i) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i)
                half4 cutoutMask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
#if defined(HARD_OCCLUSION) || defined(SOFT_OCCLUSION)
                float occlusionValue = META_DEPTH_GET_OCCLUSION_VALUE(i, _EnvironmentDepthBias);
#else
                float occlusionValue = 1.0f;
#endif
                if(cutoutMask.a == 0)
                    occlusionValue = 1.0f;
                return half4(0, 0, 0, occlusionValue);
            }
            ENDHLSL
        }
    }
}
