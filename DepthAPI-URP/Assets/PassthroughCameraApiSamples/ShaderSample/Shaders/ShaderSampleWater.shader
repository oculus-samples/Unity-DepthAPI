Shader "Meta/PCA/ShaderSampleWater" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _DetailMap ("Texture", 2D) = "white" {}
        _WaveAmplitude ("Wave Amplitude", Float) = 0.1
        _NormalOffsetX ("Normal Offset X", Float) = 0.01
        _NormalOffsetY ("Normal Offset Y", Float) = 0.01
        _Color ("Color", Color) = (1, 1, 1, 1)
        _ReflectIntensity ("Reflection Intensity", Float) = 1.0
    }

    SubShader {
        Tags {"RenderType"="Opaque"}
        LOD 100

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewDir : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _NormalMap;
            float4 _NormalMap_ST;

            sampler2D _DetailMap;
            float4 _DetailMap_ST;

            float _WaveAmplitude;
            float _NormalOffsetX;
            float _NormalOffsetY;

            fixed4 _Color;

            float _ReflectIntensity;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.viewDir = normalize(UnityWorldSpaceViewDir(v.vertex.xyz));
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // Align the UV to the screen
                float2 screenUV = i.screenPos.xy / i.screenPos.w;

                // Control waves speed
                float nX = ((_NormalOffsetX * _SinTime.y));
                float nY = ((_NormalOffsetY * _SinTime.y));
                float3 normal = tex2D(_NormalMap, i.uv + float2(nX,nY)).rgb;

                // Set the wave distorsion
                float2 distortedUV = screenUV + normal.xy * (_WaveAmplitude/100);
                float2 detailDistortedUV = i.uv + normal.xy * (_WaveAmplitude/100);

                // Mirror the texture
                distortedUV.y = 1 - distortedUV.y;

                // Set the color
                fixed4 reflectionCol = tex2D(_MainTex, distortedUV) * _ReflectIntensity;
                fixed4 col = reflectionCol * tex2D(_DetailMap,detailDistortedUV) * _Color;
                col.a = 1;
                return col;
            }
            ENDCG
        }
    }
}
