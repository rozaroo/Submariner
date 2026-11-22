Shader "Submariner/Tutorial Book Outline"
{
    Properties
    {
        [HDR] _Outline_Color ("Outline Color", Color) = (1, 0.8271, 0, 1)
        _Outline_Size ("Outline Size", Float) = 1.1
        _Outline_Glow ("Outline Glow", Range(0, 1)) = 0.16
        [HideInInspector] _Outline_Cull ("Cull", Float) = 1
        [HideInInspector] _Outline_ColorMask ("Color Mask", Float) = 15
        [HideInInspector] _Outline_StencilCompare ("Stencil Compare", Float) = 6
        [HideInInspector] _Outline_StencilPass ("Stencil Pass", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Cull [_Outline_Cull]
            ZTest LEqual
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask [_Outline_ColorMask]

            Stencil
            {
                Ref 1
                ReadMask 1
                WriteMask 1
                Comp [_Outline_StencilCompare]
                Pass [_Outline_StencilPass]
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Outline_Color;
                float _Outline_Size;
                float _Outline_Glow;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS * _Outline_Size);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(_Outline_Color.rgb * (1.0 + _Outline_Glow), _Outline_Color.a);
            }
            ENDHLSL
        }
    }
}
