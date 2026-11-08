// Jan Biernacki, 3275061; Malte Lührs, 3138900; Kürsat Yurdakul, 3282791; Paul Prigge, 3097391
Shader "GazeLab/Toon"
{
    Properties
    {
        _LightDir ("Light direction (world, towards the light)", Vector) = (-0.45, 0.62, -0.65, 0)
        _ShadowColor ("Shadow tint", Color) = (0.34, 0.39, 0.62, 1)
        _RimColor ("Rim light (alpha = strength)", Color) = (0.36, 0.75, 0.85, 0.32)
        _Gloss ("Highlight strength", Range(0, 1)) = 0.35
        _OutlineColor ("Outline", Color) = (0.04, 0.07, 0.1, 1)
        _OutlineWidth ("Outline width (world units)", Float) = 0.045
        _DepthSquash ("Depth squash", Float) = 0.025
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        CGINCLUDE
        #include "UnityCG.cginc"

        float _DepthSquash;

        float4 SquashedClipPos(float3 worldPos)
        {
            float pivotZ = unity_ObjectToWorld._m23;
            worldPos.z = pivotZ + (worldPos.z - pivotZ) * _DepthSquash;
            return mul(UNITY_MATRIX_VP, float4(worldPos, 1));
        }
        ENDCG

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 _LightDir;
            float4 _ShadowColor;
            float4 _RimColor;
            float _Gloss;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
                float2 glow : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float glow : TEXCOORD1;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = SquashedClipPos(mul(unity_ObjectToWorld, v.vertex).xyz);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.glow = v.glow.x;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.normal);
                float3 l = normalize(_LightDir.xyz);
                float3 v = normalize(UNITY_MATRIX_V[2].xyz);
                float ndl = dot(n, l);

                float tone = 0.55 * smoothstep(-0.14, -0.06, ndl) + 0.45 * smoothstep(0.42, 0.5, ndl);
                float3 albedo = i.color.rgb;
                float3 color = albedo * lerp(_ShadowColor.rgb, 1.0, tone);

                float highlight = smoothstep(0.93, 0.95, dot(n, normalize(l + v))) * i.color.a;
                float rim = smoothstep(0.68, 0.78, 1.0 - saturate(dot(n, v)));
                color += highlight * _Gloss + rim * _RimColor.rgb * _RimColor.a;

                color = lerp(color, albedo * 1.2 + 0.1, saturate(i.glow));
                return fixed4(color, 1);
            }
            ENDCG
        }

        Pass
        {
            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 _OutlineColor;
            float _OutlineWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 outlineNormal : TEXCOORD2;
            };

            float4 vert(appdata v) : SV_POSITION
            {
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 n = mul((float3x3)unity_ObjectToWorld, v.outlineNormal);
                worldPos += normalize(n + 1e-6) * _OutlineWidth;
                return SquashedClipPos(worldPos);
            }

            fixed4 frag() : SV_Target { return _OutlineColor; }
            ENDCG
        }
    }

    Fallback Off
}
