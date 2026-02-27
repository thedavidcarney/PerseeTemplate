Shader "Custom/RainbowFill"
{
    Properties
    {
        _MainTex ("Binary Mask", 2D) = "black" {}
        _AccumTex ("Accumulation Buffer", 2D) = "black" {}
        _HueSpeed ("Hue Speed", Float) = 0.5
        _Saturation ("Saturation", Float) = 1.0
        _Brightness ("Brightness", Float) = 1.0
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
            sampler2D _AccumTex;
            float _HueSpeed;
            float _Saturation;
            float _Brightness;

            // HSV to RGB conversion
            float3 HsvToRgb(float h, float s, float v)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(float3(h, h, h) + K.xyz) * 6.0 - K.www);
                return v * lerp(K.xxx, saturate(p - K.xxx), s);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float mask = tex2D(_MainTex, i.uv).r;
                float4 accum = tex2D(_AccumTex, i.uv);

                // Cycle hue over time
                float hue = frac(_Time.y * _HueSpeed);
                float3 rainbowColor = HsvToRgb(hue, _Saturation, _Brightness);

                // Where mask is white, write rainbow color on top of accum
                float3 result = lerp(accum.rgb, rainbowColor, step(0.5, mask));
                return fixed4(result, 1.0);
            }
            ENDCG
        }
    }
}
