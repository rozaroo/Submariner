Shader "Submariner/Hull Leak Bubble"
{
    Properties
    {
        _Tint ("Tint", Color) = (0.68, 0.86, 0.9, 0.82)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }

        Pass
        {
            Name "Bubble"
            Tags { "LightMode" = "UniversalForwardOnly" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color * _Tint;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 centered = (input.uv * 2.0 - 1.0) * float2(1.06, 1.0);
                float radius = length(centered);
                float feather = max(fwidth(radius) * 0.65, 0.025);
                float body = 1.0 - smoothstep(0.86 - feather, 0.86 + feather, radius);
                float rim = body * smoothstep(0.65 - feather, 0.70 + feather, radius);
                float shine = 1.0 - smoothstep(0.06, 0.24, length(centered - float2(-0.32, 0.43)));
                half alpha = saturate(body * 0.08 + rim * 0.59 + shine * body * 0.55);
                half3 color = lerp(input.color.rgb, half3(1.0, 1.0, 1.0), shine * 0.65);
                return half4(color, input.color.a * alpha);
            }
            ENDHLSL
        }
    }
}
