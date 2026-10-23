Shader "Custom/ScepterBeam"
{
    Properties
    {
        _CoreColor("Core Color", Color) = (1, 1, 1, 1)
        _GlowColor("Glow Color", Color) = (0.25, 0.85, 1, 1)
        _Intensity("Intensity (HDR)", Range(0, 10)) = 3
        _CoreWidth("Core Width", Range(0.01, 1)) = 0.25
        _PulseCount("Pulses Along Beam", Range(0, 20)) = 6
        _PulseSpeed("Pulse Speed", Range(0, 10)) = 2.5
        _PulseStrength("Pulse Strength", Range(0, 1)) = 0.5
        _FilamentWaves("Filament Waves Along Beam", Range(0, 20)) = 4
        _FilamentAmplitude("Filament Amplitude", Range(0, 0.5)) = 0.22
        _FilamentWidth("Filament Width", Range(0.01, 0.3)) = 0.07
        _MotionScale("Motion Scale", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        // Additive light ribbon for a LineRenderer (Stretch texture mode:
        // uv.x runs from the sceptre tip to the item, uv.y across the width).
        // Vertex colour alpha carries the beam's fade in/out.
        Pass
        {
            Name "Beam"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half4 _GlowColor;
                half _Intensity;
                half _CoreWidth;
                half _PulseCount;
                half _PulseSpeed;
                half _PulseStrength;
                half _FilamentWaves;
                half _FilamentAmplitude;
                half _FilamentWidth;
                half _MotionScale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float along = input.uv.x;
                float across = input.uv.y;
                float time = _Time.y * _MotionScale;
                half centered = abs(across * 2.0 - 1.0);

                // Soft cyan body with a white-hot core along the centre line.
                half glow = pow(saturate(1.0 - centered), 2.0);
                half core = smoothstep(_CoreWidth, 0.0, centered);

                // Energy pulses travelling from the sceptre towards the item.
                half pulse = sin((along * _PulseCount - time * _PulseSpeed) * TWO_PI) * 0.5 + 0.5;
                half pulses = lerp(1.0, pulse, _PulseStrength);

                // Two counter-phase filaments snaking along the beam.
                float wave = (along * _FilamentWaves - time * _PulseSpeed * 0.5) * TWO_PI;
                half filamentA = smoothstep(_FilamentWidth, 0.0, abs(across - (0.5 + _FilamentAmplitude * sin(wave))));
                half filamentB = smoothstep(_FilamentWidth, 0.0, abs(across - (0.5 - _FilamentAmplitude * sin(wave))));
                half filaments = max(filamentA, filamentB);

                // Soften the start at the sceptre tip; the item hides the far end.
                half endFade = smoothstep(0.0, 0.05, along);

                half3 rgb = _GlowColor.rgb * glow * pulses
                    + _CoreColor.rgb * (core * 0.8 + filaments) * pulses;
                return half4(rgb * _Intensity * endFade * input.color.a, 1.0);
            }
            ENDHLSL
        }
    }
}
