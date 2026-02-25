Shader "Custom/SDFJump"
{
    Properties
    {
        _MainTex ("Seed Texture", 2D) = "black" {}
        _StepSize ("Step Size", Float) = 1.0
        _TexelSize ("Texel Size", Vector) = (0, 0, 0, 0)
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
            float _StepSize;
            float4 _TexelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 nearestSeed = float2(-1, -1);
                float nearestDist = 1e10;

                // Sample 8 neighbors + self at current step size
                for(int x = -1; x <= 1; x++)
                {
                    for(int y = -1; y <= 1; y++)
                    {
                        float2 sampleUV = i.uv + float2(x, y) * _StepSize * _TexelSize.xy;
                        float2 seed = tex2D(_MainTex, sampleUV).rg;

                        if(seed.x >= 0)
                        {
                            float dist = distance(i.uv, seed);
                            if(dist < nearestDist)
                            {
                                nearestDist = dist;
                                nearestSeed = seed;
                            }
                        }
                    }
                }

                return float4(nearestSeed.x, nearestSeed.y, 0, 1);
            }
            ENDCG
        }
    }
}