// Version 2: follows the start of Pygmalion's Spectacles.
// Head-locked, inside-facing sphere, driven by PygmalionTransition.cs:
//   _Fill       liquid rises from the bottom of the view (the room stays visible through it)
//   _Cloud      the liquid clouds milky white in patches, and clears again the same way
//   _Liquid     strength of the faint clear-liquid tint (faded out at the very end)
//   _DstBlend   set by the script: 0  = hide the Unity world, so the real room shows through
//                                  10 = show the world underneath the liquid and the cloud
Shader "Custom/PygmalionLiquid"
{
    Properties
    {
        _Fill ("Liquid Level", Range(0, 1)) = 0
        _Cloud ("Cloud (milky white)", Range(0, 1)) = 0
        _Liquid ("Liquid Tint Strength", Range(0, 1)) = 1
        _LiquidTint ("Liquid Tint", Color) = (0.75, 0.88, 1.0, 0.18)
        _Milk ("Milk Color", Color) = (0.94, 0.96, 1.0, 1)
        _WaveDir ("Wave Direction (world)", Vector) = (0, 1, 0, 0)
        _WaveAmp ("Wave Amplitude", Float) = 0.04
        _WaveFreq ("Wave Frequency", Float) = 10
        _WaveSpeed ("Wave Speed", Float) = 2
        [HideInInspector] _DstBlend ("Dst Blend", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+100" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Liquid"
            Tags { "LightMode" = "UniversalForward" }

            // Premultiplied output. With _DstBlend = 0 this replaces whatever is behind it
            // (alpha 0 = real room). With _DstBlend = 10 it blends over the world.
            Blend One [_DstBlend]
            Cull Front
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Fill;
                float _Cloud;
                float _Liquid;
                half4 _LiquidTint;
                half4 _Milk;
                float4 _WaveDir;
                float _WaveAmp;
                float _WaveFreq;
                float _WaveSpeed;
                float _DstBlend;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dirWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 centerWS = TransformObjectToWorld(float3(0, 0, 0));
                o.positionCS = TransformWorldToHClip(posWS);
                o.dirWS = posWS - centerWS;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 dir = normalize(i.dirWS);
                float t = _Time.y;

                // Liquid below a rising, wobbling water line (stays level when you tilt your head)
                float h = dot(dir, normalize(_WaveDir.xyz));
                float level = lerp(-1.25, 1.25, _Fill);
                float wave = sin((dir.x + dir.z) * _WaveFreq + t * _WaveSpeed) * _WaveAmp;
                float d = h - (level + wave);                        // < 0 = under the liquid
                float liquid = 1.0 - smoothstep(-0.004, 0.004, d);
                float foam = liquid * (1.0 - smoothstep(0.0, 0.04, -d));

                // Faint shimmer on the clear liquid
                float shimmer = 0.8 + 0.4 * sin(dir.x * 9.0 + dir.y * 7.0 + t * 1.5);
                float clearA = _LiquidTint.a * _Liquid * shimmer;

                // Milky cloud that fills in (and thins out) in patches
                float n = 0.5 + 0.30 * sin(dir.x * 6.0 + dir.y * 4.0 + t * 0.20)
                              + 0.20 * sin(dir.z * 9.0 - dir.x * 5.0 - t * 0.15);
                float cloud = smoothstep(n - 0.15, n + 0.15, lerp(-0.2, 1.2, _Cloud));

                half3 rgb = lerp(_LiquidTint.rgb, _Milk.rgb, cloud);
                half a = lerp(clearA, 1.0, cloud);
                rgb = lerp(rgb, half3(1, 1, 1), foam * 0.6 * (1.0 - cloud));
                a = saturate(a + foam * 0.3 * _Liquid);
                a *= liquid;

                return half4(rgb * a, a);
            }
            ENDHLSL
        }
    }
}
