Shader "Custom/SDFDistance"
{
    Properties
    {
        _MainTex ("JFA Result", 2D) = "black" {}
        _MaxDist ("Max Distance", Float) = 0.5
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
            float _MaxDist;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 nearestSeed = tex2D(_MainTex, i.uv).rg;

                if(nearestSeed.x < 0)
                    return float4(1, 0, 0, 1); // No seed found, max distance

                float dist = distance(i.uv, nearestSeed);
                float normalizedDist = saturate(dist / _MaxDist);
                return float4(normalizedDist, 0, 0, 1);
            }
            ENDCG
        }
    }
}