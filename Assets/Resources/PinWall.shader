Shader "Custom/PinWall"
{
    Properties
    {
        _Color      ("Color",     Color)  = (0.8, 0.8, 0.8, 1)
        _Metallic   ("Metallic",  Range(0,1)) = 0.8
        _Smoothness ("Smoothness",Range(0,1)) = 0.8
        _Emission   ("Emission",  Color)  = (0, 0, 0, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "LightMode"="ForwardBase" }
        LOD 300

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_instancing
            #pragma target 4.5

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            // Per-instance transform buffer written by compute shader
            StructuredBuffer<float4x4> _Transforms;

            fixed4  _Color;
            float   _Metallic;
            float   _Smoothness;
            fixed4  _Emission;

            struct appdata
            {
                float4 vertex  : POSITION;
                float3 normal  : NORMAL;
                uint   instanceID : SV_InstanceID;
            };

            struct v2f
            {
                float4 pos     : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos    : TEXCOORD1;
                SHADOW_COORDS(2)
            };

            v2f vert(appdata v)
            {
                v2f o;

                // Get per-instance transform from buffer
                float4x4 mat = _Transforms[v.instanceID];

                // Transform vertex to world space using instance matrix
                float4 worldPos = mul(mat, v.vertex);
                o.pos       = mul(UNITY_MATRIX_VP, worldPos);
                o.worldPos  = worldPos.xyz;

                // Transform normal — use inverse transpose of upper 3x3
                float3x3 rot = (float3x3)mat;
                o.worldNormal = normalize(mul(rot, v.normal));

                TRANSFER_SHADOW(o);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 normal   = normalize(i.worldNormal);
                float3 viewDir  = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);

                // Diffuse
                float NdotL   = max(0, dot(normal, lightDir));
                float3 diffuse = _LightColor0.rgb * _Color.rgb * NdotL;

                // Specular (Blinn-Phong)
                float3 halfDir  = normalize(lightDir + viewDir);
                float  NdotH    = max(0, dot(normal, halfDir));
                float  specPow  = exp2(_Smoothness * 10.0 + 1.0);
                float3 specular = _LightColor0.rgb * _Metallic * pow(NdotH, specPow);

                // Ambient
                float3 ambient = ShadeSH9(float4(normal, 1.0)) * _Color.rgb;

                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);

                float3 col = ambient + (diffuse + specular) * atten + _Emission.rgb;
                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
