Shader "Custom/DepthNormalize"
{
    Properties
    {
        _MainTex ("Depth Texture", 2D) = "black" {}
        _MaxDepthMM ("Max Depth MM", Float) = 10000.0
        _FlipX ("Flip X", Int) = 1
        _FlipY ("Flip Y", Int) = 1
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
            float _MaxDepthMM;
            int _FlipX;
            int _FlipY;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float2 uv = v.uv;
                if(_FlipX) uv.x = 1.0 - uv.x;
                if(_FlipY) uv.y = 1.0 - uv.y;
                o.uv = uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float raw = tex2D(_MainTex, i.uv).r;
                // R16 normalizes 0-65535 to 0-1
                // Convert back to mm then normalize against maxDepthMM
                float depthMM = raw * 65535.0;
                float depthNorm = depthMM / _MaxDepthMM;
                // Zero means invalid reading, keep as zero
                float valid = step(0.0001, raw);
                return fixed4(depthNorm * valid, 0, 0, 1);
            }
            ENDCG
        }
    }
}