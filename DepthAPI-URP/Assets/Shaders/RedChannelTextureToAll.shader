Shader "Unlit/RedChannelTextureToAll"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "Assets/Shaders/Includes/DepthRangeGlobals.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float r = tex2D(_MainTex, i.uv).r;

                // Outside the band? return black
                if (r < _DepthMinMeters || r > _DepthMaxMeters) {
                    return float4(0.0, 0.0, 0.0, 1.0);
                }

                // Normalize r to [0,1] within [min, max]
                float denom = max(_DepthMaxMeters - _DepthMinMeters, 1e-6);   // avoid div-by-zero
                float n = saturate((r - _DepthMinMeters) / denom);

                // Invert (remove this line if you don't want inversion)
                n = 1.0 - n;

                return float4(n, n, n, 1.0);
            }

            ENDCG
        }
    }
}
