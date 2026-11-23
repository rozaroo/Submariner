Shader "Submariner/Pickup Silhouette Outline"
{
    Properties
    {
        _Outline_Color ("Outline Color", Color) = (1, 1, 1, 0.42)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        ZTest Always

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _Outline_Color;
        CBUFFER_END

        float EyeDepth(float rawDepth)
        {
            return IsPerspectiveProjection() ? LinearEyeDepth(rawDepth, _ZBufferParams)
                : LinearDepthToEyeDepth(rawDepth);
        }

        float DepthTolerance(float distance)
        {
            return max(0.006, distance * 0.002);
        }
        ENDHLSL

        Pass
        {
            Name "Pickup Silhouette Mask"
            Blend One One
            BlendOp Max

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex MaskVertex
            #pragma fragment MaskFragment

            float4 MaskVertex(float3 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS);
            }

            float MaskFragment(float4 positionCS : SV_POSITION) : SV_Target
            {
                float itemDepth = EyeDepth(positionCS.z);
                float sceneDepth = EyeDepth(LoadSceneDepth(uint2(positionCS.xy)));
                clip(sceneDepth + DepthTolerance(itemDepth) - itemDepth);

                // Zero means no silhouette. A larger value is closer to the camera.
                return rcp(1.0 + itemDepth);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Pickup Silhouette Outline"
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask RGB

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment

            TEXTURE2D_X_FLOAT(_PickupOutlineMask);
            static const int2 Neighbors[8] =
            {
                int2(1, 0), int2(-1, 0), int2(0, 1), int2(0, -1),
                int2(1, 1), int2(1, -1), int2(-1, 1), int2(-1, -1)
            };

            float4 OutlineVertex(uint vertexID : SV_VertexID) : SV_POSITION
            {
                return GetFullScreenTriangleVertexPosition(vertexID);
            }

            half4 OutlineFragment(float4 positionCS : SV_POSITION) : SV_Target
            {
                int2 pixel = int2(positionCS.xy);
                if (LOAD_TEXTURE2D_X(_PickupOutlineMask, pixel).r > 0.0) return 0;

                int2 lastPixel = int2(_ScaledScreenParams.xy) - 1;
                float sceneDepth = EyeDepth(LoadSceneDepth(uint2(pixel)));
                float occlusionLimit = rcp(1.0 + sceneDepth + DepthTolerance(sceneDepth));
                half coverage = 0.0h;
                [unroll]
                for (int i = 0; i < 8; i++)
                {
                    int2 neighbor = clamp(pixel + Neighbors[i], int2(0, 0), lastPixel);
                    float maskDepth = LOAD_TEXTURE2D_X(_PickupOutlineMask, neighbor).r;
                    half weight = i < 4 ? 1.0h : 0.65h;
                    coverage = max(coverage, weight * step(occlusionLimit, maskDepth));
                }

                return half4(_Outline_Color.rgb, _Outline_Color.a * coverage);
            }
            ENDHLSL
        }
    }
}
