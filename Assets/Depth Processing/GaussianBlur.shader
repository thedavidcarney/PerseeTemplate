Shader "Custom/GaussianBlur"
{
    Properties
    {
        _MainTex ("Input", 2D) = "black" {}
        _TexelSize ("Texel Size", Vector) = (0, 0, 0, 0)
        _BlurRadius ("Blur Radius", Int) = 4
        _Horizontal ("Horizontal", Int) = 1
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
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _TexelSize;
            int _BlurRadius;
            int _Horizontal;

            // Gaussian weights up to radius 8
            float GaussianWeight(int offset, int radius)
            {
                float sigma = radius * 0.5;
                return exp(-(offset * offset) / (2.0 * sigma * sigma));
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 dir = _Horizontal ? float2(_TexelSize.x, 0) : float2(0, _TexelSize.y);
                float total = 0;
                float weightSum = 0;

                for(int k = -_BlurRadius; k <= _BlurRadius; k++)
                {
                    float w = GaussianWeight(k, _BlurRadius);
                    total += tex2D(_MainTex, i.uv + dir * k).r * w;
                    weightSum += w;
                }

                return fixed4(total / weightSum, 0, 0, 1);
            }
            ENDCG
        }
    }
}