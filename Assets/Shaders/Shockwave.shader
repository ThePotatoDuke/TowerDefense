// Explosion shockwave and blast-radius marker, drawn additively so it glows with bloom.
// Both shapes are drawn on a quad, snapped to a grid of _Texels pixels across, so they read as pixel art:
// Dome (_Ring = 0): a camera-facing disc, brightest at its rim. Centered on the ground, so the ground
//                   hides its lower half and it reads as a dome.
// Ring (_Ring = 1): a flat ring lying on the ground, with a faint fill inside. A second ring can sweep
//                   outward from the middle (_Sweep 0..1; negative turns it off).
// Opacity is stepped into a few levels.
Shader "TowerDefense/Shockwave"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (2.5, 1.4, 0.6, 1)
        _Alpha ("Alpha", Range(0, 1)) = 1
        [Toggle] _Ring ("Ring (flat on the ground)", Float) = 0
        _RingWidth ("Ring Width", Range(0.01, 1)) = 0.12
        _Fill ("Inner Fill", Range(0, 1)) = 0.12
        _RimPower ("Dome Rim Power", Float) = 4
        _Sweep ("Sweeping Ring (0-1, negative = off)", Float) = -1
        _Steps ("Alpha Steps", Float) = 4
        _Texels ("Pixels Across", Float) = 64
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Blend SrcAlpha One
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
                half _Alpha;
                half _Ring;
                half _RingWidth;
                half _Fill;
                half _RimPower;
                half _Sweep;
                half _Steps;
                float _Texels;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                return o;
            }

            // 1 on a band of the given half-width around a distance from the middle, fading to 0 at its edges
            half Band(float distance, float center, float halfWidth)
            {
                return saturate(1.0 - abs(distance - center) / halfWidth);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Measure from the middle of this pixel's grid cell, so the whole cell gets one value
                float2 uv = (floor(input.uv * _Texels) + 0.5) / _Texels;
                float distance = length(uv - 0.5) * 2.0; // 0 at the middle, 1 at the edge
                if (distance > 1.0)
                    discard;

                half alpha;
                if (_Ring > 0.5)
                {
                    float halfWidth = _RingWidth * 0.5;
                    half band = Band(distance, 1.0 - halfWidth, halfWidth);
                    if (_Sweep >= 0.0)
                        band = max(band, Band(distance, _Sweep, halfWidth) * 0.75);
                    alpha = max(band, _Fill);
                }
                else
                {
                    alpha = pow(distance, _RimPower) + _Fill;
                }

                alpha = ceil(saturate(alpha) * _Steps) / _Steps * _Alpha;
                return half4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
