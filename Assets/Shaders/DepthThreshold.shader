Shader "Custom/DepthThreshold"
{
    Properties
    {
        _MainTex ("Depth Texture", 2D) = "black" {}
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
                // Flip UV 180 degrees
                o.uv = float2(1.0 - v.uv.x, 1.0 - v.uv.y);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 raw = tex2D(_MainTex, i.uv);
                float depthNorm = raw.r * 6.5535;
                
                // Zero means invalid, show as black
                float valid = step(0.0001, raw.r);
                
                // Remap depth within min/max range to 0-1 grayscale
                float gray = (depthNorm - _MinDepth) / (_MaxDepth - _MinDepth);
                gray = saturate(gray) * valid;
                
                // Pixels outside range show as black
                float inRange = step(_MinDepth, depthNorm) * step(depthNorm, _MaxDepth);
                gray *= inRange;
                
                return fixed4(gray, gray, gray, 1.0);
            }
            ENDCG
        }
    }
}