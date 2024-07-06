Shader "Environment/Checkpoint"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [HideInInspector]
        _Checkpoint ("Checkpoint (Index, MaxIndex)", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

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
            float4 _MainTex_ST;

            float4 _Checkpoint;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float checker(float2 p)
            {
                //found somewhere else on shadertoy
                // filter kernel
                float2 w = fwidth(p) + 0.01;
                // analytical integral (box filter)
                float2 i = 2.0 * (abs(frac((p - 0.5 * w)) - 0.5) - abs(frac((p + 0.5 * w)) - 0.5)) / w;
                // xor pattern
                return 0.5 - 0.5 * i.x * i.y;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = (i.uv * 2) - float2(1, 1);
                float r2 = uv.x * uv.x + uv.y * uv.y;

                if (_Checkpoint.x < _Checkpoint.y)
                {
                    float angle = atan2(uv.y, uv.x) * _Time.x * 5;

                    float2 t = float2(
                        (angle) / 300 + r2 / 8,
                        0.1 / r2 + (_Time.x * 3)
                    );

                    float d = saturate(3.0 * r2);
                    fixed4 col = tex2D(_MainTex, t) * float4(d, d, d, r2 > 0.7 && r2 < 1 ? r2 : 0);

                    return col;
                }
                else
                {
                    uv *= 4;
                    uv.y += sin(uv.x + cos(_Time.y) * 2.0) / 2.0;
                    uv.x -= sin(uv.y + _Time.y) / 2.0;
                    float3 col = lerp(float3(0.1, 0.05, 0.2), float3(1.0, 0.9, 0.8), checker(uv));
                    return fixed4(col, r2 > 0.7 && r2 < 1 ? r2 : 0);
                }
            }
            ENDCG
        }
    }
}
