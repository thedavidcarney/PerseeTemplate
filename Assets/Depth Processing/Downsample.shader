Shader "Custom/Downsample"
{
    Properties
    {
        _MainTex ("Input", 2D) = "black" {}
        _TexelSize ("Texel Size", Vector) = (0, 0, 0, 0)
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 4-tap box filter at source texel size for clean half-res downsample
                float2 t = _TexelSize.xy * 0.5;
                float s0 = tex2D(_MainTex, i.uv + float2(-t.x, -t.y)).r;
                float s1 = tex2D(_MainTex, i.uv + float2( t.x, -t.y)).r;
                float s2 = tex2D(_MainTex, i.uv + float2(-t.x,  t.y)).r;
                float s3 = tex2D(_MainTex, i.uv + float2( t.x,  t.y)).r;
                float val = (s0 + s1 + s2 + s3) * 0.25;
                return fixed4(val, 0, 0, 1);
            }
            ENDCG
        }
    }
}
