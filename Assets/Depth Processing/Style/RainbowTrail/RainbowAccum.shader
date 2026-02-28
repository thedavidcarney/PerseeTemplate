Shader "Custom/RainbowAccum"
{
    Properties
    {
        _MainTex           ("Current Composite", 2D) = "black" {}
        _FadeSpeed         ("Fade Speed",         Float) = 0.05
        _SmearX            ("Smear X",            Float) = 0.0
        _SmearY            ("Smear Y",            Float) = 0.0
        _SmearAmount       ("Smear Amount",        Float) = 0.0
        _TurbulenceScale   ("Turbulence Scale",    Float) = 5.0
        _TurbulenceSpeed   ("Turbulence Speed",    Float) = 1.0
        _TurbulenceAmount  ("Turbulence Amount",   Float) = 0.0
        _TexelSize         ("Texel Size",          Vector) = (0, 0, 0, 0)
        _TrailBlur         ("Trail Blur",          Float) = 0.0
        _UseMotionVectors  ("Use Motion Vectors",  Float) = 0.0
        _MotionVectorTex   ("Motion Vector Tex",   2D) = "gray" {}
        _FlipMVX           ("Flip MV X",           Float) = 0.0
        _FlipMVY           ("Flip MV Y",           Float) = 0.0
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
            sampler2D _MotionVectorTex;
            float _FadeSpeed;
            float _SmearX;
            float _SmearY;
            float _SmearAmount;
            float _TurbulenceScale;
            float _TurbulenceSpeed;
            float _TurbulenceAmount;
            float4 _TexelSize;
            float _TrailBlur;
            float _UseMotionVectors;
            float _FlipMVX;
            float _FlipMVY;

            float2 Turbulence(float2 uv, float scale, float speed)
            {
                float t = _Time.y * speed;
                float nx = sin(uv.x * scale + t * 1.3 + uv.y * scale * 0.7)
                         + sin(uv.y * scale * 1.7 + t * 0.9);
                float ny = sin(uv.y * scale + t * 1.1 + uv.x * scale * 0.5)
                         + sin(uv.x * scale * 1.3 + t * 1.7);
                return float2(nx, ny) * 0.5;
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
                float2 uv = i.uv;

                if(_UseMotionVectors > 0.5)
                {
                    float4 mv  = tex2D(_MotionVectorTex, i.uv);
                    float2 vel = (mv.rg - 0.5) * 2.0;
                    float  mag = mv.b;

                    // Apply optional axis flips
                    vel.x *= (_FlipMVX > 0.5) ? -1.0 : 1.0;
                    vel.y *= (_FlipMVY > 0.5) ? -1.0 : 1.0;

                    // Subtract to push trails in direction of motion
                    uv -= vel * mag * _SmearAmount * _TexelSize.xy * 100.0;
                }
                else
                {
                    uv += float2(_SmearX, _SmearY) * _SmearAmount * _TexelSize.xy * 100.0;
                }

                // Turbulence displacement
                float2 turb = Turbulence(i.uv, _TurbulenceScale, _TurbulenceSpeed);
                uv += turb * _TurbulenceAmount * _TexelSize.xy * 20.0;

                // Optional trail blur
                float4 col;
                if(_TrailBlur > 0.001)
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

                col.rgb *= (1.0 - _FadeSpeed);
                return fixed4(col.rgb, 1.0);
            }
            ENDCG
        }
    }
}
