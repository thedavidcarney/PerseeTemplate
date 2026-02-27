Shader "Custom/DepthGhost"
{
    Properties
    {
        _MainTex ("Current Frame", 2D) = "black" {}
        _HistoryTex ("History", 2D) = "black" {}
        _BlendFactor ("Blend Factor", Float) = 0.1
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
            sampler2D _HistoryTex;
            float _BlendFactor;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float current = tex2D(_MainTex, i.uv).r;
                float history = tex2D(_HistoryTex, i.uv).r;
                // Blend toward current — higher BlendFactor = faster response, less smoothing
                float result = lerp(history, current, _BlendFactor);
                return fixed4(result, 0, 0, 1);
            }
            ENDCG
        }
    }
}
