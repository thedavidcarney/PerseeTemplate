Shader "Custom/Outline"
{
    Properties
    {
        _MainTex   ("Input", 2D)          = "black" {}
        _TexelSize ("Texel Size", Vector) = (0, 0, 0, 0)
        _Thickness ("Thickness", Float)   = 2.0
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
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv     : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4    _TexelSize;
            float     _Thickness;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float self = tex2D(_MainTex, i.uv).r;

                // If already filled, output black
                if(self > 0.5)
                    return fixed4(0, 0, 0, 1);

                float2 t = _TexelSize.xy * _Thickness;

                // Sample 8 directions + 4 diagonals at thickness distance
                // Fixed sample count — no loop, no unroll issue
                float n  = tex2D(_MainTex, i.uv + float2( 0,  1) * t).r;
                float s  = tex2D(_MainTex, i.uv + float2( 0, -1) * t).r;
                float e  = tex2D(_MainTex, i.uv + float2( 1,  0) * t).r;
                float w  = tex2D(_MainTex, i.uv + float2(-1,  0) * t).r;
                float ne = tex2D(_MainTex, i.uv + float2( 1,  1) * t * 0.707).r;
                float nw = tex2D(_MainTex, i.uv + float2(-1,  1) * t * 0.707).r;
                float se = tex2D(_MainTex, i.uv + float2( 1, -1) * t * 0.707).r;
                float sw = tex2D(_MainTex, i.uv + float2(-1, -1) * t * 0.707).r;

                // Find brightest neighbor — use its value as the outline color
                float best = max(max(max(n, s), max(e, w)),
                                 max(max(ne, nw), max(se, sw)));

                if(best <= 0.5)
                    return fixed4(0, 0, 0, 1);

                return fixed4(best, best, best, 1);
            }
            ENDCG
        }
    }
}
