Shader "CityDrive/Tire Smoke"
{
    Properties
    {
        _MainTex ("Soft Smoke", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 0.45)
        _Softness ("Depth Softness", Range(0.1, 4.0)) = 1.2
    }

    SubShader
    {
        Tags { "Queue"="Transparent+5" "RenderType"="Transparent" "IgnoreProjector"="True" }
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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Softness;

            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float eyeZ : TEXCOORD2;
                UNITY_FOG_COORDS(3)
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenPos = ComputeScreenPos(o.pos);
                o.eyeZ = -mul(UNITY_MATRIX_MV, v.vertex).z;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed4 c = tex * i.color * _Color;

                // Soft-particle fade against the road/buildings; prevents hard cutters.
                float2 screenUv = i.screenPos.xy / max(0.0001, i.screenPos.w);
                float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUv), _ZBufferParams);
                float fade = 1.0;
                if (sceneZ > 0.001)
                    fade = saturate((sceneZ - i.eyeZ) / max(0.05, _Softness));
                c.a *= fade;

                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }
            ENDCG
        }
    }

    Fallback Off
}
