Shader "Snowboarder/Trail"
{
    Properties
    {
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
        _SunDirection ("Sun Direction", Vector) = (0, -1, 0, 0)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct InVertex
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct V2F
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            sampler2D _NormalMap;
            float4 _NormalMap_ST;
            float4 _Color;
            float3 _SunDirection; // TODO: use _WorldSpaceLightPos0

            V2F vert (InVertex v)
            {
                V2F o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _NormalMap);

                o.worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                return o;
            }

            fixed4 frag (V2F i) : SV_Target
            {
                // Sample the normal map
                fixed3 normal = tex2D(_NormalMap, i.uv).rgb;
                normal = normal * 2.0 - 1.0; // Convert from [0,1] to [-1,1]

                normal = normalize(mul((float3x3)unity_ObjectToWorld, normal));
                fixed3 combinedNormal = normalize(i.worldNormal + normal);

                // Compute the lighting
                float NdotL = dot(combinedNormal, _SunDirection);

                return float4(_Color.rgb/* * ((NdotL + 1) / 2)*/, max(0.1, _Color.a * abs(NdotL)));
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}