Shader "Custom/CarryableRim"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.8, 0.8, 0.8, 1)
        _BaseMap("Base Map", 2D) = "white" {}
        _OcclusionMap("Occlusion Map", 2D) = "white" {}
        _RoughnessMap("Roughness Map (linear)", 2D) = "white" {}
        _RimColor("Rim Color", Color) = (0.25, 0.85, 1, 1)
        _RimStrength("Rim Strength", Range(0, 1)) = 0
        _RimPower("Rim Power", Range(0.5, 8)) = 3
        _OutlineColor("Outline Color", Color) = (0.25, 0.85, 1, 1)
        _OutlineWidth("Outline Width", Range(0, 0.05)) = 0.015
        _FlashColor("Flash Color", Color) = (1, 0.85, 0.3, 1)
        _FlashAmount("Flash Amount", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        // Main surface: textured lambert + specular (smoothness from inverted
        // roughness map) + a fresnel rim whose strength the scepter drives.
        Pass
        {
            Name "Unlit"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _RimColor;
                half _RimStrength;
                half _RimPower;
                half4 _OutlineColor;
                half _OutlineWidth;
                half4 _FlashColor;
                half _FlashAmount;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_OcclusionMap);
            SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_RoughnessMap);
            SAMPLER(sampler_RoughnessMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                half3 viewDirWS = normalize(_WorldSpaceCameraPos - input.positionWS);

                half3 albedo = _BaseColor.rgb * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                half occlusion = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).r;
                half smoothness = 1.0 - SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, input.uv).r;

                // Main-light lambert + ambient, scaled by baked AO.
                Light mainLight = GetMainLight();
                half ndl = saturate(dot(normalWS, mainLight.direction));
                half3 lighting = mainLight.color * ndl * occlusion + SampleSH(normalWS) * occlusion;

                // Cheap blinn-phong specular from the inverted roughness map.
                half3 halfDir = normalize(mainLight.direction + viewDirWS);
                half specular = pow(saturate(dot(normalWS, halfDir)), smoothness * 96.0 + 8.0) * smoothness;

                // Fresnel edge glow: with a high _RimPower this hugs the silhouette.
                half fresnel = 1.0 - saturate(dot(normalWS, viewDirWS));
                half rim = pow(fresnel, _RimPower) * _RimStrength;

                half3 rgb = albedo * lighting
                    + mainLight.color * specular * occlusion
                    + _RimColor.rgb * rim * 2.0
                    + _FlashColor.rgb * _FlashAmount * 2.0;
                return half4(rgb, 1.0);
            }
            ENDHLSL
        }

        // Inverted-hull outline: backfaces pushed along vertex normals render as a
        // glowing band around the silhouette. The scepter aims through the same
        // _RimStrength property, so the outline fades in with the rim and is fully
        // transparent (alpha 0) otherwise.
        Pass
        {
            Name "Outline"

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _RimColor;
                half _RimStrength;
                half _RimPower;
                half4 _OutlineColor;
                half _OutlineWidth;
                half4 _FlashColor;
                half _FlashAmount;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionOS = input.positionOS.xyz + input.normalOS * _OutlineWidth;
                output.positionHCS = TransformObjectToHClip(positionOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // During a validation flash the outline goes gold and glows
                // alongside the rim.
                half3 outlineColor = lerp(_OutlineColor.rgb, _FlashColor.rgb, saturate(_FlashAmount));
                half alpha = saturate(_RimStrength + _FlashAmount);
                return half4(outlineColor * 2.0, alpha);
            }
            ENDHLSL
        }
    }
}
