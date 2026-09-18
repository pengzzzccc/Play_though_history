Shader "Custom/ScepterAimRing"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.25, 0.85, 1, 1)
        _Intensity("Intensity", Float) = 2
        _RingRadius("Ring Radius", Range(0, 1)) = 0.75
        _RingWidth("Ring Width", Range(0.01, 0.5)) = 0.12
        _DashCount("Dash Count", Range(0, 64)) = 12
        _SpinSpeed("Spin Speed", Float) = 90
        _PulseSpeed("Pulse Speed", Float) = 3
        _MotionAmount("Motion Amount", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Unlit"

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Intensity;
                half _RingRadius;
                half _RingWidth;
                half _DashCount;
                half _SpinSpeed;
                half _PulseSpeed;
                half _MotionAmount;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 centered = input.uv * 2.0 - 1.0;
                float dist = length(centered);

                // Soft-edged ring.
                float ring = 1.0 - smoothstep(0.0, _RingWidth, abs(dist - _RingRadius));

                // Rotating dash segments along the ring (frozen when MotionAmount is 0).
                float angle = atan2(centered.y, centered.x);
                float dash = 0.55 + 0.45 * sin(angle * _DashCount + _Time.y * _SpinSpeed * _MotionAmount);

                // Gentle breathing pulse.
                float pulse = 1.0 + 0.25 * sin(_Time.y * _PulseSpeed) * _MotionAmount;

                // Fade before the quad edge so the square never shows.
                float edgeFade = smoothstep(1.0, 0.95, dist);

                float alpha = ring * dash * edgeFade;
                half3 color = _BaseColor.rgb * _Intensity * pulse;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
