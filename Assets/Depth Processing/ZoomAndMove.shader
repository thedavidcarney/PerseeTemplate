Shader "Custom/ZoomAndMove"
{
    Properties
    {
        _MainTex ("Input", 2D) = "black" {}
        _OffsetX ("Offset X", Float) = 0.0
        _OffsetY ("Offset Y", Float) = 0.0
        _Zoom ("Zoom", Float) = 1.0
        _ScaleX ("Scale X", Float) = 1.0
        _ScaleY ("Scale Y", Float) = 1.0
        _Rotation ("Rotation", Float) = 0.0
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
            float _OffsetX;
            float _OffsetY;
            float _Zoom;
            float _ScaleX;
            float _ScaleY;
            float _Rotation;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Center UV around 0
                float2 uv = i.uv - 0.5;

                // Apply rotation (degrees to radians)
                float rad = _Rotation * 0.01745329;
                float cosR = cos(rad);
                float sinR = sin(rad);
                uv = float2(uv.x * cosR - uv.y * sinR, uv.x * sinR + uv.y * cosR);

                // Apply non-uniform scale
                uv.x /= _ScaleX;
                uv.y /= _ScaleY;

                // Apply uniform zoom
                uv /= _Zoom;

                // Apply offset (pan)
                uv -= float2(_OffsetX, _OffsetY);

                // Shift back to 0-1 space
                uv += 0.5;

                // Discard pixels outside source bounds
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                    return fixed4(0, 0, 0, 1);

                float val = tex2D(_MainTex, uv).r;
                return fixed4(val, 0, 0, 1);
            }
            ENDCG
        }
    }
}
