uniform UNITY_DECLARE_TEX2DARRAY(_EnvironmentDepthTexture);

float SampleEnvironmentDepth(float2 reprojectedUV) {
  return UNITY_SAMPLE_TEX2DARRAY(_EnvironmentDepthTexture,
           float3(reprojectedUV, (float)unity_StereoEyeIndex)).r;
}

#include "Packages/com.meta.xr.depthapi/Runtime/Core/Shaders/EnvironmentOcclusion.cginc"

float CalculateEnvironmentDepthOcclusion(float2 uv, float sceneDepth) {
  #if !(defined(HARD_OCCLUSION) || defined(SOFT_OCCLUSION))
    return 1.0f;
  #endif

  #if defined(HARD_OCCLUSION)
    return 1.0f - CalculateEnvironmentDepthHardOcclusion_Internal(uv, LinearEyeDepth(sceneDepth));
  #elif defined(SOFT_OCCLUSION)
    return 1.0f - CalculateEnvironmentDepthSoftOcclusion_Internal(uv, LinearEyeDepth(sceneDepth));
  #endif

  return 1.0f;
}
