Shader "Void of Tides/UI/Pause Blur"
{
    Properties
    {
        [PerRendererData] _MainTex ("UI Texture", 2D) = "white" {}
        _SceneTexture ("Gameplay Camera Texture", 2D) = "black" {}
        _BlurRadius ("Blur Radius", Range(1, 24)) = 16
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            Texture2D _SceneTexture;
            SamplerState sampler_LinearClamp;
            float4 _SceneTexture_TexelSize;
            float _BlurRadius;

            struct Attributes
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.position);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // A spiral distributes the samples across the blur disk without a repeating grid.
                const float2 offsets[16] = {
                    float2(0.177, 0.000), float2(-0.226, 0.207), float2(0.035, -0.394), float2(0.285, 0.371),
                    float2(-0.522, -0.092), float2(0.495, -0.315), float2(-0.165, 0.616), float2(-0.316, -0.608),
                    float2(0.685, 0.250), float2(-0.712, 0.294), float2(0.343, -0.734), float2(0.254, 0.809),
                    float2(-0.765, -0.443), float2(0.897, -0.197), float2(-0.548, 0.779), float2(-0.126, -0.976)
                };
                float2 radius = abs(_SceneTexture_TexelSize.xy) * _BlurRadius;
                half3 blurred = 0;

                [unroll] for (int i = 0; i < 16; i++)
                {
                    float2 uv = input.uv + offsets[i] * radius;
                    blurred += _SceneTexture.Sample(sampler_LinearClamp, uv).rgb;
                }

                return half4(blurred / 16, 1) * input.color;
            }
            ENDHLSL
        }
    }
}
