// Pixel-art sky, laid out on the screen like a 2D game's backdrop, so it shows wherever the background
// is visible (past the edges of the ground) however steeply the camera looks down. Two tones of blue meet
// in an ordered (Bayer) dither band, and pixel clouds drift slowly across the upper part of the screen.
// Each cloud picks one of the three cloud textures and a height at random; three layers drifting at
// different speeds keep the pattern from repeating visibly.
// Set as the scene's skybox material. Heights are fractions of the screen: 0 at the bottom, 1 at the top.
Shader "TowerDefense/PixelSky"
{
    Properties
    {
        _TopColor ("Upper Sky", Color) = (0.28, 0.49, 0.9, 1)
        _BottomColor ("Lower Sky", Color) = (0.48, 0.7, 0.98, 1)
        _BlendStart ("Dither Band Bottom (screen height)", Range(0, 1)) = 0.6
        _BlendEnd ("Dither Band Top (screen height)", Range(0, 1)) = 0.92
        _DitherPixel ("Dither Pixel Size (screen pixels)", Float) = 4

        [NoScaleOffset] _CloudA ("Cloud Texture 1", 2D) = "black" {}
        [NoScaleOffset] _CloudB ("Cloud Texture 2", 2D) = "black" {}
        [NoScaleOffset] _CloudC ("Cloud Texture 3", 2D) = "black" {}
        _CloudPixel ("Cloud Pixel Size (screen pixels)", Float) = 4
        _CloudSpacing ("Cloud Slot Width (screen pixels)", Float) = 420
        _CloudChance ("Chance a Slot Has a Cloud", Range(0, 1)) = 0.6
        _CloudSpeed ("Drift Speed (screen pixels per second)", Float) = 10
        _CloudBottom ("Cloud Area Bottom (screen height)", Range(0, 1)) = 0.62
        _CloudTop ("Cloud Area Top (screen height)", Range(0, 1)) = 0.98
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }

        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_CloudA);
            SAMPLER(sampler_CloudA);
            TEXTURE2D(_CloudB);
            SAMPLER(sampler_CloudB);
            TEXTURE2D(_CloudC);
            SAMPLER(sampler_CloudC);

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _BottomColor;
                float _BlendStart;
                float _BlendEnd;
                float _DitherPixel;
                float4 _CloudA_TexelSize;
                float4 _CloudB_TexelSize;
                float4 _CloudC_TexelSize;
                float _CloudPixel;
                float _CloudSpacing;
                float _CloudChance;
                float _CloudSpeed;
                float _CloudBottom;
                float _CloudTop;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.direction = input.positionOS.xyz; // the skybox mesh surrounds the camera
                return o;
            }

            float Hash(float n)
            {
                return frac(sin(n * 12.9898) * 43758.5453);
            }

            // 4x4 ordered dither threshold for a cell, between 0 and 1
            float Bayer4(float2 cell)
            {
                static const float matrix4[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
                int2 p = int2(fmod(cell, 4.0));
                return (matrix4[p.y * 4 + p.x] + 0.5) / 16.0;
            }

            float4 CloudTexelSize(int index)
            {
                return index == 0 ? _CloudA_TexelSize : (index == 1 ? _CloudB_TexelSize : _CloudC_TexelSize);
            }

            half4 SampleCloud(int index, float2 uv)
            {
                if (index == 0) return SAMPLE_TEXTURE2D_LOD(_CloudA, sampler_CloudA, uv, 0);
                if (index == 1) return SAMPLE_TEXTURE2D_LOD(_CloudB, sampler_CloudB, uv, 0);
                return SAMPLE_TEXTURE2D_LOD(_CloudC, sampler_CloudC, uv, 0);
            }

            // One drifting layer of clouds, cut into equal slots across the screen. Each slot may hold one
            // cloud: which texture, where across the slot and how high are all picked from the slot number.
            // Returns the sky with this layer's cloud (if any) drawn over it. Positions are in screen pixels.
            half3 CloudLayer(half3 sky, float2 screenPixel, float layer)
            {
                float speed = _CloudSpeed * (0.6 + layer * 0.3);       // nearer layers drift faster
                float spacing = _CloudSpacing * (1.0 + layer * 0.37);  // and are spaced differently
                float across = screenPixel.x + _Time.y * speed + layer * 1234.0;
                float slot = floor(across / spacing);
                float inSlot = across - slot * spacing;

                float seed = Hash(slot * 1.618 + layer * 17.31);
                if (seed > _CloudChance)
                    return sky;

                int index = (int)floor(frac(seed * 9.7) * 3.0) % 3;
                float4 texelSize = CloudTexelSize(index);
                float2 size = texelSize.zw * _CloudPixel; // cloud size on screen

                float left = frac(seed * 13.7) * max(0.0, spacing - size.x);
                float areaBottom = _CloudBottom * _ScreenParams.y;
                float areaTop = _CloudTop * _ScreenParams.y - size.y;
                float bottom = lerp(areaBottom, max(areaBottom, areaTop), frac(seed * 5.13));

                float2 uv = float2(inSlot - left, screenPixel.y - bottom) / size;
                if (any(uv < 0.0) || any(uv > 1.0))
                    return sky;

                // Whole texels only, so the clouds stay crisp pixel art
                uv = (floor(uv * texelSize.zw) + 0.5) * texelSize.xy;
                half4 cloud = SampleCloud(index, uv);
                return lerp(sky, cloud.rgb, cloud.a);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Where this direction sits on screen, from the camera's own view and field of view:
                // 0 at the bottom edge, 1 at the top (independent of how the render target is flipped)
                float3 viewDirection = mul((float3x3)UNITY_MATRIX_V, normalize(input.direction));
                float depth = max(0.0001, -viewDirection.z);
                float screenX = 0.5 + 0.5 * (viewDirection.x / depth) * abs(UNITY_MATRIX_P[0][0]);
                float screenY = 0.5 + 0.5 * (viewDirection.y / depth) * abs(UNITY_MATRIX_P[1][1]);
                float2 screenPixel = float2(screenX, screenY) * _ScreenParams.xy;

                // Two tones, mixed through an ordered dither across the band between them
                float blend = saturate((screenY - _BlendStart) / max(0.001, _BlendEnd - _BlendStart));
                float threshold = Bayer4(floor(screenPixel / max(1.0, _DitherPixel)));
                half3 sky = blend > threshold ? _TopColor.rgb : _BottomColor.rgb;

                // Far layer first, so nearer clouds pass in front
                sky = CloudLayer(sky, screenPixel, 0.0);
                sky = CloudLayer(sky, screenPixel, 1.0);
                sky = CloudLayer(sky, screenPixel, 2.0);

                return half4(sky, 1.0);
            }
            ENDHLSL
        }
    }
}
