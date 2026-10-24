// Pixel-art trail for TrailRenderers.
// The ribbon is cut into square blocks the size of one sprite pixel, and every block takes the
// color, fade and coverage of its center, so the trail has stair-stepped edges and a stepped fade
// instead of smooth gradients. Drawn just before other transparents, so it stays behind sprites.
Shader "TowerDefense/PixelTrail"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1, 1, 1, 1)
        _PixelSize ("Pixel Size (world units)", Float) = 0.0625
        _AlphaSteps ("Alpha Steps", Float) = 4
        _Inset ("Edge Inset (fraction of width per side)", Range(0, 0.45)) = 0.25
    }

    SubShader
    {
        Tags { "Queue" = "Transparent-1" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _PixelSize;
                float _AlphaSteps;
                float _Inset;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.uv = input.uv;
                o.color = input.color;
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // How many screen pixels one sprite pixel covers here
                float worldPerScreenPixel = max(length(ddx(input.positionWS)), 1e-5);
                float block = max(1.0, _PixelSize / worldPerScreenPixel);

                // Offset from this fragment to the center of its block
                float2 screen = input.positionCS.xy;
                float2 offset = (floor(screen / block) + 0.5) * block - screen;

                // Values at the block center, extrapolated with screen-space derivatives
                float2 uv = input.uv + ddx(input.uv) * offset.x + ddy(input.uv) * offset.y;
                half4 color = input.color + ddx(input.color) * offset.x + ddy(input.color) * offset.y;

                // uv.y runs across the ribbon; the inset leaves room for whole blocks at the edges
                if (uv.y < _Inset || uv.y > 1.0 - _Inset)
                    discard;

                half alpha = ceil(saturate(color.a) * _AlphaSteps) / _AlphaSteps;
                if (alpha <= 0.0)
                    discard;

                return half4(color.rgb * _Color.rgb, alpha * _Color.a);
            }
            ENDHLSL
        }
    }
}
