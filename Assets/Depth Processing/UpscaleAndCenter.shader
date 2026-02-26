Shader "Custom/UpscaleAndCenter"
{
    Properties
    {
        _MainTex ("Input", 2D) = "black" {}
        // UV rect of the source image within the destination:
        // x = left edge, y = bottom edge, z = width, w = height (all in 0-1 destination UV space)
        _SrcRect ("Source Rect", Vector) = (0, 0, 1, 1)
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
            float4 _SrcRect; // x=left, y=bottom, z=width, w=height in dest UV space

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Check if this dest pixel falls within the source rect
                float2 rectMin = _SrcRect.xy;
                float2 rectMax = _SrcRect.xy + _SrcRect.zw;

                if (i.uv.x < rectMin.x || i.uv.x > rectMax.x ||
                    i.uv.y < rectMin.y || i.uv.y > rectMax.y)
                {
                    return fixed4(0, 0, 0, 1); // outside — black
                }

                // Remap dest UV back to source UV
                float2 srcUV = (i.uv - rectMin) / _SrcRect.zw;
                float val = tex2D(_MainTex, srcUV).r;
                return fixed4(val, 0, 0, 1);
            }
            ENDCG
        }
    }
}
