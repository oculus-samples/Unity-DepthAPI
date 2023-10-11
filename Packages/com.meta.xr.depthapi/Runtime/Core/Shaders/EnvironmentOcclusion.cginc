uniform float4x4 _EnvironmentDepthReprojectionMatrices[2];
uniform float4 _EnvironmentDepthZBufferParams;

#define SAMPLE_OFFSET_PIXELS 6.0f
#define RELATIVE_ERROR_SCALE 0.015f

float2 CalculateScreenSpaceOffset() {
  return SAMPLE_OFFSET_PIXELS / _ScreenParams.xy;
}

/**
 * SampleEnvironmentDepth(reprojectedUV) has to be provided from outside of this file.
 * For example, EnvironmentOcclusionBiRP.cginc or EnvironmentOcclusionURP.hlsl
 */

float SampleEnvironmentDepthReprojected(float2 uv) {
  const float4 reprojectedUV =
      mul(_EnvironmentDepthReprojectionMatrices[unity_StereoEyeIndex], float4(uv.x, uv.y, 0.0, 1.0));

  const float inputDepthEye = SampleEnvironmentDepth(reprojectedUV);

  const float inputDepthNdc = inputDepthEye * 2.0 - 1.0;
  const float linearDepth = (1.0f / (inputDepthNdc + _EnvironmentDepthZBufferParams.y)) * _EnvironmentDepthZBufferParams.x;

  return linearDepth;
}

// Not guarded by occlusion keywords, use CalculateEnvironmentDepthOcclusion
float CalculateOcclusionValue_Internal(float2 depthUv, float sceneDepth) {
  const float environmentDepth = SampleEnvironmentDepthReprojected(depthUv);
  const float relativeError = environmentDepth / sceneDepth - 1;
  const float scaledOcclusionValue = relativeError / RELATIVE_ERROR_SCALE;

  // occlusion is 0.5 if environment == sceneDepth
  return clamp(0.5 - 0.5 * scaledOcclusionValue, 0.0, 1.0);
}

// Not guarded by occlusion keywords, use CalculateEnvironmentDepthOcclusion
float CalculateEnvironmentDepthHardOcclusion_Internal(float2 depthUv,
                         float sceneDepth) {
  return SampleEnvironmentDepthReprojected(depthUv) < sceneDepth;
}

// Not guarded by occlusion keywords, use CalculateEnvironmentDepthOcclusion
float CalculateEnvironmentDepthSoftOcclusion_Internal(float2 uv, float sceneDepth) {
  #define TEST_SAMPLES 4

  static const uint POISSON_SAMPLES = 11;
  static const float2 poissonDisk[POISSON_SAMPLES] = {
    float2( 0.9789267005017435f, 0.14401885022921376f ),
    float2( -0.9043537867387434f, -0.3606923565241508f ),
    float2( 0.311521973180601f, -0.9132562206107396f ),
    float2( -0.2305685898894408f, 0.9070738255481436f ),
    float2( -0.8865688823018791f, 0.4204923507525414f ),
    float2( 0.4020375708925012f, 0.8941011867274369f ),
    float2( -0.33142350418249433f, -0.8611052735427547f ),
    float2( 0.6716543228214223f, -0.426789950875022f ),
    float2( -0.3604733289097599f, 0.11357279143380132f ),
    float2( 0.23595175158496592f, 0.26950844847277605f ),
    float2( 0.033190036742041545f, -0.3504560360183565f )
  };

  const float2 scale = CalculateScreenSpaceOffset();
  float result = 0.0;

  UNITY_UNROLL
  for (uint i = 0; i < TEST_SAMPLES; ++i) {
    const float2 offset = poissonDisk[i] * scale;
    result += CalculateOcclusionValue_Internal(uv + offset, sceneDepth);
  }

  UNITY_BRANCH
  if ((TEST_SAMPLES - result ) * result < 0.001f) {
    return result / TEST_SAMPLES;
  }

  UNITY_UNROLL
  for (uint i = TEST_SAMPLES; i < POISSON_SAMPLES; ++i) {
    const float2 offset = poissonDisk[i] * scale;
    result += CalculateOcclusionValue_Internal(uv + offset, sceneDepth);
  }

  return result / POISSON_SAMPLES;
}
