Shader "Custom/BoxBlur"
{
    Properties
    {
        _MainTex ("Input", 2D) = "black" {}
        _TexelSize ("Texel Size", Vector) = (0, 0, 0, 0)
        _Radius ("Radius", Int) = 1
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
            float4 _TexelSize;
            int _Radius;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float total = 0;
                int count = 0;
                for(int x = -_Radius; x <= _Radius; x++)
                {
                    for(int y = -_Radius; y <= _Radius; y++)
                    {
                        total += tex2D(_MainTex, i.uv + float2(x, y) * _TexelSize.xy).r;
                        count++;
                    }
                }
                return fixed4(total / count, 0, 0, 1);
            }
            ENDCG
        }
    }
}
