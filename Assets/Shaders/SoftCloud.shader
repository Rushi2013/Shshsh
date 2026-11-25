Shader "CityDrive/Soft Cloud"
{
    Properties
    {
        _Tint ("Sunlit Color", Color) = (1.0, 0.99, 0.96, 1)
        _ShadowColor ("Shade Color", Color) = (0.58, 0.62, 0.68, 1)
        _Opacity ("Opacity", Range(0.0, 1.0)) = 0.86
        _Softness ("Edge Softness", Range(0.05, 1.0)) = 0.55
        _NoiseScale ("Detail Scale", Range(0.005, 0.2)) = 0.045
        _Drift ("Detail Drift", Range(0.0, 0.2)) = 0.012
        _FadeNear ("Near Fade", Float) = 120
        _FadeFar ("Far Fade", Float) = 900
    }

    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            fixed4 _Tint;
            fixed4 _ShadowColor;
            float _Opacity;
            float _Softness;
            float _NoiseScale;
            float _Drift;
            float _FadeNear;
            float _FadeFar;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normalWorld : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normalWorld = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            float Hash(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float Noise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n000 = Hash(i);
                float n100 = Hash(i + float3(1,0,0));
                float n010 = Hash(i + float3(0,1,0));
                float n110 = Hash(i + float3(1,1,0));
                float n001 = Hash(i + float3(0,0,1));
                float n101 = Hash(i + float3(1,0,1));
                float n011 = Hash(i + float3(0,1,1));
                float n111 = Hash(i + float3(1,1,1));
                return lerp(
                    lerp(lerp(n000,n100,f.x), lerp(n010,n110,f.x), f.y),
                    lerp(lerp(n001,n101,f.x), lerp(n011,n111,f.x), f.y), f.z);
            }

            float Fbm(float3 p)
            {
                float v = 0.0;
                float a = 0.55;
                for (int i = 0; i < 3; i++)
                {
                    v += a * Noise(p);
                    p = p * 2.07 + float3(11.3, 7.1, 3.7);
                    a *= 0.5;
                }
                return v;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 normal = normalize(i.normalWorld);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float facing = saturate(dot(normal, viewDir));

                float3 np = i.worldPos * _NoiseScale +
                            float3(_Time.y * _Drift, _Time.y * _Drift * 0.25, 0.0);
                float detail = Fbm(np);
                float edge = smoothstep(0.0, _Softness, facing + (detail - 0.5) * 0.42);
                float density = saturate(0.52 + detail * 0.62);

                float distanceToEye = distance(_WorldSpaceCameraPos, i.worldPos);
                float farFade = 1.0 - smoothstep(_FadeNear, _FadeFar, distanceToEye);
                float nearFade = smoothstep(20.0, 90.0, distanceToEye);
                float alpha = _Opacity * edge * density * farFade * nearFade;

                float3 sunDir = normalize(_WorldSpaceLightPos0.xyz);
                float lit = saturate(dot(normal, sunDir) * 0.7 + 0.35);
                float3 col = lerp(_ShadowColor.rgb, _Tint.rgb, lit);
                col *= lerp(0.92, 1.08, detail);

                fixed4 output = fixed4(col, alpha);
                UNITY_APPLY_FOG(i.fogCoord, output);
                return output;
            }
            ENDCG
        }
    }

    Fallback Off
}
