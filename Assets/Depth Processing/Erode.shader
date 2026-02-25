Shader "Custom/Erode"
{
    Properties
    {
        _MainTex ("Input", 2D) = "black" {}
        _KernelSize ("Kernel Size", Int) = 1
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
            int _KernelSize;
            float4 _TexelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Erosion: if any pixel in the kernel is black, output is black
                float result = 1.0;
                for(int x = -_KernelSize; x <= _KernelSize; x++)
                {
                    for(int y = -_KernelSize; y <= _KernelSize; y++)
                    {
                        float2 offset = float2(x, y) * _TexelSize.xy;
                        float sample = tex2D(_MainTex, i.uv + offset).r;
                        result = min(result, sample);
                    }
                }
                return fixed4(result, 0, 0, 1);
            }
            ENDCG
        }
    }
}