Shader "Hidden/Void of Tides/Underwater Blur"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "Underwater Blur"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _UnderwaterParams;
            float4 _UnderwaterPixelSize;
            half4 _OceanColor;

            half4 SampleScene(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0);
            }

            float WaterDistance(float2 uv)
            {
                float depth = SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                    depth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, depth);
                #endif
                float3 scenePosition = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                float3 ray = scenePosition - GetCameraPositionWS();
                // For upward rays, stop accumulating haze where the view exits the water.
                float immersedFraction = saturate(_UnderwaterParams.z / max(ray.y, 0.001));
                return min(length(ray) * immersedFraction, 25.0);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 texel = _UnderwaterPixelSize.xy;
                float2 uv = input.texcoord;
                float2 ripple = sin(uv.yx * float2(33.0, 26.0) + _Time.y * float2(1.35, -1.1));
                uv += ripple * texel * (0.55 * _UnderwaterParams.y);

                float2 stepSize = texel * _UnderwaterParams.x;
                float2 x = float2(stepSize.x, 0.0);
                float2 y = float2(0.0, stepSize.y);
                half4 color = SampleScene(uv) * 4.0h;
                color += (SampleScene(uv + x) + SampleScene(uv - x)
                    + SampleScene(uv + y) + SampleScene(uv - y)) * 2.0h;
                color += SampleScene(uv + x + y) + SampleScene(uv + x - y)
                    + SampleScene(uv - x + y) + SampleScene(uv - x - y);
                color *= 0.0625h;

                // Water absorbs reds more quickly than greens and blues.
                float opticalDepth = (0.55 + WaterDistance(input.texcoord)) * _UnderwaterParams.w;
                half3 transmission = exp(-opticalDepth * float3(2.7, 1.12, 0.70));
                half3 underwaterColor = lerp(_OceanColor.rgb, color.rgb, transmission);
                color.rgb = lerp(color.rgb, underwaterColor, sqrt(_UnderwaterParams.y));
                return color;
            }
            ENDHLSL
        }
    }
}
