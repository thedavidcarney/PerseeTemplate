Shader "Custom/SDFSeed"
{
    Properties
    {
        _MainTex ("Binary Mask", 2D) = "black" {}
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float mask = tex2D(_MainTex, i.uv).r;
                // White pixels encode their own UV as seed position
                // Black pixels encode sentinel value (-1, -1) meaning no seed
                if(mask > 0.5)
                    return float4(i.uv.x, i.uv.y, 0, 1);
                else
                    return float4(-1, -1, 0, 1);
            }
            ENDCG
        }
    }
}