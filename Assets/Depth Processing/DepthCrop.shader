Shader "Custom/DepthCrop"
{
    Properties
    {
        _MainTex ("Input", 2D) = "black" {}
        _MinDepth ("Min Depth", Float) = 0.0
        _MaxDepth ("Max Depth", Float) = 1.0
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
            float _MinDepth;
            float _MaxDepth;

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
                // Zero is invalid, always crop out
                float valid = step(0.0001, depth);
                // Zero out anything outside min/max range
                float inRange = step(_MinDepth, depth) * step(depth, _MaxDepth);
                return fixed4(depth * inRange * valid, 0, 0, 1);
            }
            ENDCG
        }
    }
}