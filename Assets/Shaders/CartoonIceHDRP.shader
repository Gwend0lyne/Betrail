Shader "Custom/CartoonIceHDRP"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.55, 0.8, 0.95, 1)
        _ShadowColor ("Shadow Color", Color) = (0.1, 0.25, 0.45, 1)
        _HighlightColor ("Highlight Color", Color) = (0.9, 1, 1, 1)
        _RimColor ("Rim Color", Color) = (0.7, 0.9, 1, 1)
        _RimPower ("Rim Power", Range(0.1, 8)) = 3
        _RimIntensity ("Rim Intensity", Range(0, 2)) = 0.9
        _DepthColor ("Depth Color", Color) = (0.2, 0.5, 0.8, 1)
        _DepthStrength ("Depth Strength", Range(0, 1)) = 0.35
        _DepthFalloff ("Depth Falloff", Range(0.2, 4)) = 1.2
        _ShadowSmooth ("Shadow Smoothness", Range(0.01, 1)) = 0.25
        _HighlightShift ("Highlight Shift", Range(-1, 1)) = 0.15
        _Opacity ("Opacity", Range(0, 1)) = 0.85
        [ToggleUI]_UseNormalMap ("Use Normal Map", Float) = 0
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalScale ("Normal Strength", Range(0, 2)) = 1
        _NoiseTex ("Sparkle Noise", 2D) = "white" {}
        _SparkleIntensity ("Sparkle Intensity", Range(0, 1)) = 0.3
        _SparkleScale ("Sparkle Scale", Range(0.5, 6)) = 2
        _SparkleSpeed ("Sparkle Speed", Vector) = (0.25, 0.1, 0, 0)
        _SparklePower ("Sparkle Power", Range(1, 12)) = 6
    }

    HLSLINCLUDE
    #pragma target 4.5
    #pragma warning(disable: 5208)
    #define _SURFACE_TYPE_TRANSPARENT 1
    #define _BLENDMODE_ALPHA 1
    #define _ENABLE_FOG_ON_TRANSPARENT 1
    #define USE_LEGACY_UNITY_MATRIX_VARIABLES

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

    CBUFFER_START(UnityPerMaterial)
        float4 _BaseColor;
        float4 _ShadowColor;
        float4 _HighlightColor;
        float4 _RimColor;
        float4 _DepthColor;
        float _RimPower;
        float _RimIntensity;
        float _DepthStrength;
        float _DepthFalloff;
        float _ShadowSmooth;
        float _HighlightShift;
        float _Opacity;
        float _UseNormalMap;
        float _NormalScale;
        float _SparkleIntensity;
        float _SparkleScale;
        float4 _SparkleSpeed;
        float _SparklePower;
    CBUFFER_END

    TEXTURE2D(_NormalMap);
    SAMPLER(sampler_NormalMap);
    float4 _NormalMap_ST;

    TEXTURE2D(_NoiseTex);
    SAMPLER(sampler_NoiseTex);
    float4 _NoiseTex_ST;
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 tangentWS : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
                float2 uv : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float3 GetFallbackTangent(float3 normalWS)
            {
                float3 up = abs(normalWS.y) < 0.999f ? float3(0.0f, 1.0f, 0.0f) : float3(1.0f, 0.0f, 0.0f);
                float3 tangent = normalize(cross(up, normalWS));
                return tangent;
            }

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 positionWS = TransformObjectToWorld(IN.positionOS);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                OUT.positionWS = positionWS;
                OUT.normalWS = normalize(normalWS);
                OUT.uv = IN.uv;

                float3 tangentWS;
                float3 bitangentWS;

                if (any(IN.tangentOS.xyz))
                {
                    tangentWS = TransformObjectToWorldDir(IN.tangentOS.xyz);
                    tangentWS = normalize(tangentWS);
                    bitangentWS = cross(OUT.normalWS, tangentWS) * (IN.tangentOS.w * unity_WorldTransformParams.w);
                }
                else
                {
                    tangentWS = GetFallbackTangent(OUT.normalWS);
                    bitangentWS = normalize(cross(OUT.normalWS, tangentWS));
                }

                OUT.tangentWS = tangentWS;
                OUT.bitangentWS = bitangentWS;

                OUT.positionCS = TransformWorldToHClip(positionWS);
                return OUT;
            }

            float3 SampleNormalWS(Varyings IN)
            {
                float3 normalWS = normalize(IN.normalWS);
                if (_UseNormalMap > 0.5f)
                {
                    float2 uv = IN.uv * _NormalMap_ST.xy + _NormalMap_ST.zw;
                    float3 normalSample = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv).xyz * 2.0f - 1.0f;
                    normalSample.xy *= _NormalScale;
                    normalSample.z = sqrt(saturate(1.0f - dot(normalSample.xy, normalSample.xy)));
                    float3x3 tbn = float3x3(normalize(IN.tangentWS), normalize(IN.bitangentWS), normalWS);
                    normalWS = normalize(mul(normalSample, tbn));
                }
                return normalWS;
            }

            float3 ComputeSparkle(float2 uv, float sparkPower)
            {
                float2 sparkleUV = uv * _NoiseTex_ST.xy + _NoiseTex_ST.zw;
                sparkleUV *= _SparkleScale;
                sparkleUV += _TimeParameters.x * _SparkleSpeed.xy;
                float sparkleSample = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, sparkleUV).r;
                sparkleSample = pow(saturate(sparkleSample), sparkPower);
                return _HighlightColor.rgb * sparkleSample * _SparkleIntensity;
            }

            float4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 normalWS = SampleNormalWS(IN);
                float3 viewDirWS = normalize(_WorldSpaceCameraPos - IN.positionWS);

                float elevation = saturate(dot(normalWS, float3(0.0, 1.0, 0.0)));
                float shadowBand = smoothstep(_HighlightShift - _ShadowSmooth, _HighlightShift + _ShadowSmooth, elevation);
                float highlightBand = smoothstep(0.5f - _ShadowSmooth, 0.5f + _ShadowSmooth, elevation + _HighlightShift);

                float3 toonBase = lerp(_ShadowColor.rgb, _BaseColor.rgb, shadowBand);
                toonBase = lerp(toonBase, _HighlightColor.rgb, highlightBand);

                float fresnelTerm = pow(saturate(1.0f - dot(viewDirWS, normalWS)), _RimPower) * _RimIntensity;
                float3 fresnel = _RimColor.rgb * fresnelTerm;

                float depthTerm = pow(saturate(1.0f - dot(viewDirWS, normalWS)), _DepthFalloff) * _DepthStrength;
                float3 depthTint = _DepthColor.rgb * depthTerm;

                float3 sparkle = ComputeSparkle(IN.uv, _SparklePower);

                float3 finalColor = toonBase + fresnel + depthTint + sparkle;
                finalColor = saturate(finalColor);

                float alpha = saturate(_Opacity + fresnelTerm * 0.35f + depthTerm * 0.25f);

                return float4(finalColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
