Shader "Custom/SDFContours"
{
    Properties
    {
        _MainTex ("Distance Field", 2D) = "black" {}
        _MaskTex ("Original Mask", 2D) = "black" {}
        _Frequency ("Frequency", Float) = 10.0
        _LineWidth ("Line Width", Float) = 0.3
        _AnimSpeed ("Animation Speed", Float) = 0.5
        _InsideColor ("Inside Color", Color) = (0, 0, 0, 1)
        _OutsideColor ("Outside Color", Color) = (0.0, 0.3, 0.4, 1)
        _LineColor ("Line Color", Color) = (0.0, 0.8, 1.0, 1)
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
            sampler2D _MaskTex;
            float _Frequency;
            float _LineWidth;
            float _AnimSpeed;
            fixed4 _InsideColor;
            fixed4 _OutsideColor;
            fixed4 _LineColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float dist = tex2D(_MainTex, i.uv).r;
                float mask = tex2D(_MaskTex, i.uv).r;
                float inside = step(0.5, mask);

                float animOffset = _Time.y * _AnimSpeed;
                float bands = sin((dist * _Frequency + animOffset) * 3.14159265);
                float contourLine = (1.0 - smoothstep(0.0, _LineWidth, abs(bands))) * (1.0 - inside);

                fixed4 baseColor = lerp(_OutsideColor, _InsideColor, inside);
                fixed4 finalColor = lerp(baseColor, _LineColor, contourLine);

                return finalColor;
            }
            ENDCG
        }
    }
}