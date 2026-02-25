Shader "Custom/TemporalNoise"
{
    Properties
    {
        _MainTex ("Current Frame", 2D) = "black" {}
        _HistoryTex ("History", 2D) = "black" {}
        _HistoryWeight ("History Weight", Float) = 0.6
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
            float _HistoryWeight;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float current = tex2D(_MainTex, i.uv).r;
                float history = tex2D(_HistoryTex, i.uv).r;

                // Blend current with history
                float blended = current * (1.0 - _HistoryWeight) + history * _HistoryWeight;

                // Re-threshold at 0.5 to get clean binary output
                float result = step(0.5, blended);

                return fixed4(result, 0, 0, 1);
            }
            ENDCG
        }
    }
}