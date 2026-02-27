Shader "Custom/RainbowAccum"
{
    Properties
    {
        _MainTex ("Current Composite", 2D) = "black" {}
        _FadeSpeed ("Fade Speed", Float) = 0.05
        _SmearX ("Smear X", Float) = 0.0
        _SmearY ("Smear Y", Float) = 0.0
        _SmearAmount ("Smear Amount", Float) = 0.0
        _TurbulenceScale ("Turbulence Scale", Float) = 5.0
        _TurbulenceSpeed ("Turbulence Speed", Float) = 1.0
        _TurbulenceAmount ("Turbulence Amount", Float) = 0.0
        _TexelSize ("Texel Size", Vector) = (0, 0, 0, 0)
        _TrailBlur ("Trail Blur", Float) = 0.0
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
            float _FadeSpeed;
            float _SmearX;
            float _SmearY;
            float _SmearAmount;
            float _TurbulenceScale;
            float _TurbulenceSpeed;
            float _TurbulenceAmount;
            float4 _TexelSize;
            float _TrailBlur;

            // Cheap 2D value noise using sin
            float2 Turbulence(float2 uv, float scale, float speed)
            {
                float t = _Time.y * speed;
                float nx = sin(uv.x * scale + t * 1.3 + uv.y * scale * 0.7) 
                         + sin(uv.y * scale * 1.7 + t * 0.9);
                float ny = sin(uv.y * scale + t * 1.1 + uv.x * scale * 0.5) 
                         + sin(uv.x * scale * 1.3 + t * 1.7);
                return float2(nx, ny) * 0.5; // normalize to roughly -1..1
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
                float2 uv = i.uv;

                // Apply smear offset
                uv += float2(_SmearX, _SmearY) * _SmearAmount * _TexelSize.xy * 100.0;

                // Apply turbulence displacement
                float2 turb = Turbulence(i.uv, _TurbulenceScale, _TurbulenceSpeed);
                uv += turb * _TurbulenceAmount * _TexelSize.xy * 20.0;

                // Optional trail blur - sample 4 neighbors and average
                float4 col;
                if (_TrailBlur > 0.001)
                {
                    float b = _TrailBlur * _TexelSize.x * 10.0;
                    col  = tex2D(_MainTex, uv + float2(-b, -b));
                    col += tex2D(_MainTex, uv + float2( b, -b));
                    col += tex2D(_MainTex, uv + float2(-b,  b));
                    col += tex2D(_MainTex, uv + float2( b,  b));
                    col *= 0.25;
                }
                else
                {
                    col = tex2D(_MainTex, uv);
                }

                // Fade toward black
                col.rgb *= (1.0 - _FadeSpeed);

                return fixed4(col.rgb, 1.0);
            }
            ENDCG
        }
    }
}
