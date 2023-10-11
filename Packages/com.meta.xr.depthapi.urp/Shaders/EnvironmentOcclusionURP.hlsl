#ifndef ENVIRONMENT_DEPTH_INCLUDED
#define ENVIRONMENT_DEPTH_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#define PREFER_HALF 0
#define SHADER_HINT_NICE_QUALITY 1

TEXTURE2D_X_FLOAT(_EnvironmentDepthTexture);

SAMPLER(sampler_EnvironmentDepthTexture);
float4 _EnvironmentDepthTexture_TexelSize;

float SampleEnvironmentDepth(const float2 reprojectedUV) {
  return SAMPLE_TEXTURE2D_X(_EnvironmentDepthTexture, sampler_EnvironmentDepthTexture, reprojectedUV).r;
}

#include "Packages/com.meta.xr.depthapi/Runtime/Core/Shaders/EnvironmentOcclusion.cginc"

float CalculateEnvironmentDepthOcclusion(float2 uv, float sceneDepth) {
  #if defined(HARD_OCCLUSION)
    return 1.0f - CalculateEnvironmentDepthHardOcclusion_Internal(uv, LinearEyeDepth(sceneDepth, _ZBufferParams));
  #elif defined(SOFT_OCCLUSION)
    return 1.0f - CalculateEnvironmentDepthSoftOcclusion_Internal(uv, LinearEyeDepth(sceneDepth, _ZBufferParams));
  #endif

  return 1.0f;
}

#endif
