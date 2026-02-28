Shader "Custom/Invert"
{
    Properties
    {
        _MainTex      ("Input", 2D)  = "white" {}
        _IgnoreBlack  ("Ignore Black", Float) = 0.0
        _BlackThresh  ("Black Threshold", Float) = 0.01
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
            float _IgnoreBlack;
            float _BlackThresh;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float val = tex2D(_MainTex, i.uv).r;
                if(_IgnoreBlack > 0.5 && val < _BlackThresh)
                    return fixed4(0, 0, 0, 1);
                return fixed4(1.0 - val, 0, 0, 1);
            }
            ENDCG
        }
    }
}
