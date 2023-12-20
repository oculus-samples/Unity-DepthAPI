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
                float4 positionCS : SV_POSITION;

                // 3. This macro adds required data field to the varyings struct
                //    The number has to be filled with the recent TEXCOORD number + 1
                //    Or 0 as in this case, if there are no other TEXCOORD fields
                META_DEPTH_VERTEX_OUTPUT(0)

                UNITY_VERTEX_INPUT_INSTANCE_ID
                // 4. The fragment shader needs to understand to which eye it's currently
                //    rendering, in order to get depth from the correct texture.
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                // 5. World position is required to calculate the occlusions.
                //    This macro will calculate and set world position value in the output Varyings structure.
                META_DEPTH_INITIALIZE_VERTEX_OUTPUT(output, input.vertex);

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                // 6. Passes stereo information to frag shader
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = UnityObjectToClipPos(input.vertex);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                // 7. Initializes global stereo constant for the frag shader
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 finalColor = _BaseColor;

                // 8. A third macro required to enable occlusions.
                //    It requires previous macros to be there as well as the naming behind the macro is strict.
                //    It will enable soft or hard occlusions depending on the current keyword set.
                //    finalColor value will be multiplied by the occlusion visibility value.
                //    Occlusion visibility value is 0 if virtual object is completely covered by environment and vice versa.
                //    Fully occluded pixels will be discarded
                META_DEPTH_OCCLUDE_OUTPUT_PREMULTIPLY(input, finalColor, 0);

                return finalColor;
            }
            ENDCG
        }
    }
}
