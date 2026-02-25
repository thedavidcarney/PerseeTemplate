Shader "Custom/Threshold"
{
    Properties
    {
        _MainTex ("Input", 2D) = "black" {}
        _Threshold ("Threshold", Float) = 0.0
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
            float _Threshold;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float depth = tex2D(_MainTex, i.uv).r;
                float binary = step(_Threshold, depth) * step(0.0001, depth);
                return fixed4(binary, 0, 0, 1);
            }
            ENDCG
        }
    }
}