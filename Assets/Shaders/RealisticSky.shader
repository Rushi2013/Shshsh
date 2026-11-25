Shader "CityDrive/Realistic Sky"
{
    Properties
    {
        _Tint ("Zenith Tint", Color) = (0.36, 0.52, 0.78, 1)
        _Horizon ("Horizon Color", Color) = (0.74, 0.82, 0.90, 1)
        _Ground ("Ground Haze", Color) = (0.38, 0.39, 0.40, 1)
        _Exposure ("Exposure", Range(0.2, 2.0)) = 1.0
        _SunSize ("Sun Size", Range(0.002, 0.12)) = 0.028
        _Atmosphere ("Atmosphere", Range(0.3, 3.0)) = 1.05
        _CloudStrength ("High Cloud Haze", Range(0.0, 0.7)) = 0.22
        _CloudDrift ("Cloud Drift", Range(0.0, 0.2)) = 0.012
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Tint;
            fixed4 _Horizon;
            fixed4 _Ground;
            float _Exposure;
            float _SunSize;
            float _Atmosphere;
            float _CloudStrength;
            float _CloudDrift;

            struct appdata { float4 vertex : POSITION; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = mul((float3x3)unity_ObjectToWorld, v.vertex.xyz);
                return o;
            }

            float Hash(float3 p)
            {
                p = frac(p * 0.3183099 + float3(0.71, 0.113, 0.419));
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float Noise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = lerp(Hash(i), Hash(i + float3(1,0,0)), f.x);
                float b = lerp(Hash(i + float3(0,1,0)), Hash(i + float3(1,1,0)), f.x);
                float c = lerp(Hash(i + float3(0,0,1)), Hash(i + float3(1,0,1)), f.x);
                float d = lerp(Hash(i + float3(0,1,1)), Hash(i + float3(1,1,1)), f.x);
                return lerp(lerp(a,b,f.y), lerp(c,d,f.y), f.z);
            }

            float Fbm(float3 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int i = 0; i < 3; i++)
                {
                    v += a * Noise(p);
                    p *= 2.03;
                    a *= 0.5;
                }
                return v;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);
                float h = dir.y;

                // Atmospheric gradient, horizon haze first, then deeper blue overhead.
                float3 col = lerp(_Ground.rgb, _Horizon.rgb, smoothstep(-0.18, 0.10, h));
                col = lerp(col, _Tint.rgb, smoothstep(0.0, 0.72, h));

                float3 sunDir = normalize(_WorldSpaceLightPos0.xyz);
                float sunDot = saturate(dot(dir, sunDir));
                float disc = smoothstep(1.0 - _SunSize, 1.0, sunDot);
                float halo = pow(sunDot, 48.0);
                col += float3(1.0, 0.93, 0.78) * (disc * 1.6 + halo * 0.28);

                // Very soft high-altitude cloud veil. Real 3D cloud geometry is a separate layer.
                float3 cp = dir * (3.2 * _Atmosphere) +
                            float3(_Time.y * _CloudDrift, 0.0, _Time.y * _CloudDrift * 0.35);
                float n = Fbm(cp);
                float veil = smoothstep(0.56, 0.82, n) * smoothstep(0.05, 0.30, h) *
                             (1.0 - smoothstep(0.55, 0.98, h) * 0.45);
                float3 lit = lerp(float3(0.66, 0.69, 0.73), float3(1.0, 0.99, 0.96),
                                  saturate(sunDot * 0.9 + 0.2));
                col = lerp(col, lit, veil * _CloudStrength);

                col *= _Exposure;
                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
