Shader "Unlit/RedChannelTextureToAll"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Min ("Min (threshold)", Float) = 0.0
        _Max ("Max (threshold)", Float) = 1.0
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
            float _Min, _Max;

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
                if (r < _Min || r > _Max) {
                    return float4(0.0, 0.0, 0.0, 1.0);
                }

                // Normalize r to [0,1] within [_Min, _Max]
                float denom = max(_Max - _Min, 1e-6);   // avoid div-by-zero
                float n = saturate((r - _Min) / denom);

                // Invert (remove this line if you don't want inversion)
                n = 1.0 - n;

                return float4(n, n, n, 1.0);
            }

            ENDCG
        }
    }
}
