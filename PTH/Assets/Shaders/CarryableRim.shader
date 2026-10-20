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
        _RimPower("Rim Power", Range(1, 16)) = 6
        _RimIntensity("Rim Intensity (HDR)", Range(0, 8)) = 3
        _RimCoreWhite("Rim Core Whiteness", Range(0, 1)) = 0.6
        _OutlineColor("Glow Color", Color) = (0.25, 0.85, 1, 1)
        _OutlineWidth("Glow Width (world units)", Range(0, 0.2)) = 0.06
        _GlowIntensity("Glow Intensity (HDR)", Range(0, 8)) = 2
        _GlowFalloff("Glow Falloff", Range(0.5, 4)) = 1.5
        [ToggleUI] _UseCenterNormals("Use Center Normals (convex hard-edged meshes)", Float) = 0
        _FlashColor("Flash Color", Color) = (1, 0.85, 0.3, 1)
        _FlashAmount("Flash Amount", Range(0, 1)) = 0
    }

    SubShader
    {
        // Drawn after opaques and the skybox so the additive glow, which writes
        // no depth, is never painted over by geometry drawn later.
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Transparent-10"
            "RenderPipeline" = "UniversalPipeline"
        }

        // Main surface: textured lambert + specular (smoothness from inverted
        // roughness map) + a thin fresnel edge whose strength the scepter drives.
        // The edge brightens towards white at the silhouette and outputs HDR
        // values above 1 so bloom turns it into a glow.
        Pass
        {
            Name "Surface"
            Tags { "LightMode" = "UniversalForward" }

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
                half _RimIntensity;
                half _RimCoreWhite;
                half4 _OutlineColor;
                half _OutlineWidth;
                half _GlowIntensity;
                half _GlowFalloff;
                half _UseCenterNormals;
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
                float3 rimDirWS : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;

                // Hard-edged convex meshes (cubes) have one normal per face, so a
                // normal-based fresnel lights whole faces. The direction from the
                // object's centre gives them a sphere-like gradient instead.
                float3 centerDirWS = TransformObjectToWorldDir(SafeNormalize(input.positionOS.xyz));
                output.rimDirWS = lerp(output.normalWS, centerDirWS, _UseCenterNormals);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                half3 rimDirWS = SafeNormalize(input.rimDirWS);
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

                // Thin edge: a high _RimPower keeps the fresnel on the silhouette,
                // and the colour shifts towards white where it is strongest.
                half fresnel = 1.0 - saturate(dot(rimDirWS, viewDirWS));
                half edge = pow(fresnel, _RimPower) * _RimStrength;
                half3 edgeColor = lerp(_RimColor.rgb, half3(1.0, 1.0, 1.0), saturate(edge * _RimCoreWhite));

                half3 rgb = albedo * lighting
                    + mainLight.color * specular * occlusion
                    + edgeColor * edge * _RimIntensity
                    + _FlashColor.rgb * _FlashAmount * 2.0;
                return half4(rgb, 1.0);
            }
            ENDHLSL
        }

        // Halo: an inverted hull pushed out in world space and blended
        // additively. On the hull's back faces -dot(n, v) is largest next to the
        // silhouette and falls to zero at the hull's outer edge, so the glow
        // fades outwards. It shares _RimStrength with the edge (fully off when
        // not aimed at) and turns gold during a validation flash.
        Pass
        {
            Name "Glow"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha One
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
                half _RimIntensity;
                half _RimCoreWhite;
                half4 _OutlineColor;
                half _OutlineWidth;
                half _GlowIntensity;
                half _GlowFalloff;
                half _UseCenterNormals;
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
                float3 positionWS : TEXCOORD0;
                float3 dirWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                // Centre directions keep the hull closed on hard-edged meshes,
                // where split normals would tear it open at every corner.
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 centerDirWS = TransformObjectToWorldDir(SafeNormalize(input.positionOS.xyz));
                float3 dirWS = SafeNormalize(lerp(normalWS, centerDirWS, _UseCenterNormals));

                // World-space width: the halo looks the same at any object scale.
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz) + dirWS * _OutlineWidth;
                output.positionHCS = TransformWorldToHClip(output.positionWS);
                output.dirWS = dirWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 dirWS = SafeNormalize(input.dirWS);
                half3 viewDirWS = normalize(_WorldSpaceCameraPos - input.positionWS);
                half glow = pow(saturate(-dot(dirWS, viewDirWS)), _GlowFalloff);

                half3 glowColor = lerp(_OutlineColor.rgb, _FlashColor.rgb, saturate(_FlashAmount)) * _GlowIntensity;
                half strength = saturate(_RimStrength + _FlashAmount) * glow;
                return half4(glowColor, strength);
            }
            ENDHLSL
        }
    }
}
