Shader "Submariner/Oxygen Gauge Tube"
{
    Properties
    {
        _OxygenFill ("Oxygen Fill", Range(0, 1)) = 1
        _GasColor ("Oxygen Color", Color) = (0.26, 0.79, 0.94, 1)
        _GasVisibility ("Gas Visibility", Range(0, 3)) = 1
        _CollarColor ("Collar Color", Color) = (0.47, 0.42, 0.32, 1)
        _FlowSpeed ("Gas Flow Speed", Range(0, 2)) = 0.95
        _FillFeather ("Fill Edge Softness", Range(0, 0.02)) = 0.003
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }

        Pass
        {
            Name "Gauge"
            Tags { "LightMode" = "UniversalForwardOnly" }
            Cull Back
            ZWrite Off
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_instancing
            #define _SURFACE_TYPE_TRANSPARENT 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _GasColor;
                float4 _CollarColor;
                float _OxygenFill;
                float _GasVisibility;
                float _FlowSpeed;
                float _FillFeather;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                half3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                half3 normalOS : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.positionOS = input.positionOS;
                output.normalOS = input.normalOS;
                return output;
            }

            float SoftBand(float coordinate, float center, float width, float pixelWidth)
            {
                float filteredWidth = sqrt(width * width + pixelWidth * pixelWidth * 0.5);
                float distance = (coordinate - center) / filteredWidth;
                return exp2(-distance * distance) * (width / filteredWidth);
            }

            float NoiseHash(float3 cell)
            {
                float3 p = frac(cell * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float CloudNoise(float3 position)
            {
                float3 cell = floor(position);
                float3 blend = frac(position);
                blend = blend * blend * (3.0 - 2.0 * blend);

                float lower = lerp(
                    lerp(NoiseHash(cell), NoiseHash(cell + float3(1, 0, 0)), blend.x),
                    lerp(NoiseHash(cell + float3(0, 1, 0)), NoiseHash(cell + float3(1, 1, 0)), blend.x), blend.y);
                float upper = lerp(
                    lerp(NoiseHash(cell + float3(0, 0, 1)), NoiseHash(cell + float3(1, 0, 1)), blend.x),
                    lerp(NoiseHash(cell + float3(0, 1, 1)), NoiseHash(cell + float3(1, 1, 1)), blend.x), blend.y);
                return lerp(lower, upper, blend.z);
            }

            float GasDensity(float3 positionOS, float phase)
            {
                float3 cloud = positionOS * float3(4.5, 9.5, 4.5);
                cloud.x += sin(cloud.y * 0.60 - phase * 1.40) * 0.68;
                cloud.y += phase * 2.5;
                float baseCloud = CloudNoise(cloud);
                float detail = CloudNoise(cloud * 1.95 + float3(4.0, phase * 0.65, -6.0));
                return smoothstep(0.25, 0.75, baseCloud * 0.73 + detail * 0.27);
            }

            float2 GasInterval(float3 surface, float3 ray, float bottom, float top)
            {
                const float innerRadius = 0.405;
                float radialSpeed = dot(ray.xz, ray.xz);
                float radiusError = dot(surface.xz, surface.xz) - innerRadius * innerRadius;
                float2 interval = float2(0.0, 4.0);

                if (radialSpeed > 0.00001)
                {
                    float projection = dot(surface.xz, ray.xz);
                    float discriminant = projection * projection - radialSpeed * radiusError;
                    if (discriminant <= 0.0) return 0.0;
                    float spread = sqrt(discriminant);
                    interval = float2(max(0.0, (-projection - spread) / radialSpeed),
                        (-projection + spread) / radialSpeed);
                }
                else if (radiusError >= 0.0)
                {
                    return 0.0;
                }

                if (abs(ray.y) > 0.00001)
                {
                    float2 capHits = (float2(bottom, top) - surface.y) / ray.y;
                    interval.x = max(interval.x, min(capHits.x, capHits.y));
                    interval.y = min(interval.y, max(capHits.x, capHits.y));
                }
                else if (surface.y < bottom || surface.y > top)
                {
                    return 0.0;
                }

                return interval;
            }

            // Integral of the smoothstep ramp, used to average density through the gas edge.
            float IntegratedFeather(float position)
            {
                float t = saturate(position);
                return t * t * t * (1.0 - 0.5 * t) + max(position - 1.0, 0.0);
            }

            float FeatherCoverage(float2 height, float fillBoundary, float feather)
            {
                float2 ramp = (height - fillBoundary) / (2.0 * feather) + 0.5;
                if (abs(ramp.y - ramp.x) < 0.01)
                {
                    return smoothstep(0.0, 1.0, (ramp.x + ramp.y) * 0.5);
                }
                return saturate((IntegratedFeather(ramp.y) - IntegratedFeather(ramp.x)) / (ramp.y - ramp.x));
            }

            void AccumulateTubeLight(Light light, float3 normalWS, float3 viewWS,
                inout float3 incomingLight, inout float3 reflectedLight)
            {
                float3 lightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
                float facing = dot(normalWS, light.direction);
                incomingLight += lightColor * saturate(0.15 + 0.85 * facing);

                float3 halfDirection = SafeNormalize(light.direction + viewWS);
                float metalReflection = pow(saturate(dot(normalWS, halfDirection)), 16.0);
                reflectedLight += lightColor * (metalReflection * saturate(facing));
            }

            half3 TubeLighting(Varyings input, float3 normalWS, float3 viewWS, out half3 collarReflection)
            {
                float3 incomingLight = 0.0;
                float3 reflectedLight = 0.0;
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                AccumulateTubeLight(mainLight, normalWS, viewWS, incomingLight, reflectedLight);

                #if defined(_ADDITIONAL_LIGHTS)
                    const half4 shadowMask = half4(1, 1, 1, 1);
                    #if USE_CLUSTER_LIGHT_LOOP
                        InputData inputData = (InputData)0;
                        inputData.positionWS = input.positionWS;
                        inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                        UNITY_LOOP for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                        {
                            CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                            Light light = GetAdditionalLight(lightIndex, input.positionWS, shadowMask);
                            AccumulateTubeLight(light, normalWS, viewWS, incomingLight, reflectedLight);
                        }
                    #endif

                    uint lightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(lightCount)
                        Light light = GetAdditionalLight(lightIndex, input.positionWS, shadowMask);
                        AccumulateTubeLight(light, normalWS, viewWS, incomingLight, reflectedLight);
                    LIGHT_LOOP_END
                #endif

                // Limit the response to nearby lamps while keeping their color in the shading.
                float reflectionPeak = max(reflectedLight.r, max(reflectedLight.g, reflectedLight.b));
                collarReflection = reflectedLight * (0.14 / (reflectionPeak + 0.70));
                float peak = max(incomingLight.r, max(incomingLight.g, incomingLight.b));
                half3 diffuse = lerp(peak.xxx, incomingLight, 0.60h);
                return diffuse * (0.43 / (peak + 0.55));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // The Unity cylinder spans -1 to 1 along local Y. Its +Y end is the left side of this gauge.
                float endDistance = saturate(1.0 - abs(input.positionOS.y)) * 0.5;
                float3 viewWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float3 viewOS = TransformWorldToObjectDir(viewWS);
                float3 normalOS = normalize(input.normalOS);
                float2 viewAcross = viewOS.xz * rsqrt(max(dot(viewOS.xz, viewOS.xz), 0.00001));
                float across = dot(normalOS.xz, float2(-viewAcross.y, viewAcross.x));
                float alongPixel = fwidth(input.positionOS.y);
                float collarPixel = alongPixel * 0.5;
                float gasketWidth = max(0.004, collarPixel * 0.75);
                float collarWidth = max(0.003, collarPixel * 0.75);
                float gasket = 1.0 - smoothstep(0.045 - gasketWidth, 0.045 + gasketWidth, endDistance);
                float collar = 1.0 - smoothstep(0.038 - collarWidth, 0.038 + collarWidth, endDistance);

                // Sample moving density at two depths inside the chamber.
                float gasAlpha = 0.0;
                half3 gasColor = 0.0;
                float fill = saturate(_OxygenFill);
                float visibility = clamp(_GasVisibility, 0.0, 3.0);
                if (fill > 0.0 && visibility > 0.0 && gasket < 1.0)
                {
                    const float gasEnd = 0.910;
                    const float chamberLength = 2.0 * gasEnd;
                    float fillBoundary = gasEnd - chamberLength * fill;
                    float feather = max(_FillFeather * chamberLength, max(0.65 * alongPixel, 0.0001));
                    float2 interval = GasInterval(input.positionOS, -viewOS, max(-gasEnd, fillBoundary - feather), gasEnd);
                    float distance = interval.y - interval.x;
                    if (distance > 0.0)
                    {
                        float phase = _Time.y * _FlowSpeed;
                        float3 nearPoint = input.positionOS - viewOS * lerp(interval.x, interval.y, 0.28);
                        float3 farPoint = input.positionOS - viewOS * lerp(interval.x, interval.y, 0.74);
                        float density = GasDensity(nearPoint, phase) * 0.65 + GasDensity(farPoint, phase + 0.7) * 0.35;
                        float2 gasHeight = input.positionOS.y - viewOS.y * interval;
                        float coverage = FeatherCoverage(gasHeight, fillBoundary, feather);
                        coverage *= smoothstep(0.0, feather, chamberLength * fill);
                        float opticalDepth = distance * coverage * lerp(1.15, 2.6, density);
                        gasAlpha = 1.0 - exp2(-opticalDepth * visibility);
                        float colorIntensity = 1.0 + 0.45 * max(visibility - 1.0, 0.0);
                        gasColor = _GasColor.rgb * (lerp(0.25, 0.73, density) * colorIntensity);
                    }
                }

                float bevel = SoftBand(endDistance, 0.005, 0.0058, collarPixel)
                    + SoftBand(endDistance, 0.030, 0.0062, collarPixel);
                float groove = SoftBand(endDistance, 0.021, 0.0038, collarPixel);
                float metalBand = SoftBand(across, 0.25, 0.49, fwidth(across));
                half3 metal = _CollarColor.rgb * (0.26 + 0.57 * metalBand + 0.47 * bevel - 0.16 * groove);

                float3 normalWS = TransformObjectToWorldNormal(normalOS);
                half3 collarReflection;
                half3 lighting = TubeLighting(input, normalWS, viewWS, collarReflection);
                half3 metalColor = metal * lighting;
                metalColor += collarReflection * lerp(_CollarColor.rgb, half3(0.65, 0.62, 0.55), 0.35h)
                    * (0.28 + 0.62 * bevel);

                half3 color = gasColor * (gasAlpha * lighting);
                color = lerp(color, half3(0.024, 0.035, 0.038) * lighting, gasket);
                color = lerp(color, metalColor, collar);
                return half4(color, lerp(gasAlpha, 1.0, gasket));
            }
            ENDHLSL
        }
    }
}
