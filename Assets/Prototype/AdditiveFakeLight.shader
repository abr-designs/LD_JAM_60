
Shader "Custom/URP/AdditiveFakeLight"
{
    Properties
    {
        [HDR] _Color ("Light Color", Color) = (1, 0.8, 0.5, 1)
        _Intensity ("Intensity", Float) = 1.0
        _Radius ("Radius", Range(0.01, 1.0)) = 0.8
        _Falloff ("Falloff", Range(0.01, 1.0)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "AdditiveFakeLight"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Intensity;
                float _Radius;
                float _Falloff;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionHCS =
                    TransformObjectToHClip(input.positionOS);

                // Standard Unity quad local XY coordinates.
                output.uv = input.positionOS.xy + 0.5;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 centeredUV = input.uv - 0.5;
                float distanceFromCenter = length(centeredUV);

                float normalizedDistance =
                    distanceFromCenter / max(_Radius * 0.5, 0.001);

                float edgeStart = 1.0 - _Falloff;

                float mask = 1.0 - smoothstep(
                    edgeStart,
                    1.0,
                    normalizedDistance
                );

                half3 emission =
                    _Color.rgb * (_Intensity * mask);

                return half4(emission, 1.0);
            }

            ENDHLSL
        }
    }

    FallBack Off
}
