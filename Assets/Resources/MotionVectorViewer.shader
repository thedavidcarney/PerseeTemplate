Shader "Custom/MotionVectorViewer"
{
    Properties
    {
        _MainTex ("Vector Field", 2D) = "gray" {}
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

            float3 HsvToRgb(float h, float s, float v)
            {
                float4 K = float4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
                float3 p = abs(frac(float3(h,h,h) + K.xyz) * 6.0 - K.www);
                return v * lerp(K.xxx, saturate(p - K.xxx), s);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 vec = tex2D(_MainTex, i.uv);
                float  mag = vec.b;

                // Zero magnitude = no motion = black
                if(mag < 0.001)
                    return fixed4(0, 0, 0, 1);

                float2 vel = (vec.rg - 0.5) * 2.0;
                float  hue = atan2(vel.y, vel.x) / (2.0 * 3.14159) + 0.5;
                float3 col = HsvToRgb(hue, 1.0, mag);

                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
}
