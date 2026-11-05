// Darkness over the whole screen with a round hole in it (the Bat Cave's light around the player), for a
// full-screen UI image on its own canvas under the HUD. The edge of the hole is an ordered (Bayer) dither
// band in chunky pixels, like the sky's, so the light fades out in pixel-art steps. The image's alpha
// fades the whole thing. Positions are in screen pixels from the bottom left (_ViewSize is the screen's).
Shader "TowerDefense/DarknessHole"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Darkness", Color) = (0, 0, 0, 1)
        _Center ("Hole Centre (screen pixels)", Vector) = (960, 540, 0, 0)
        _Radius ("Hole Radius (screen pixels)", Float) = 220
        _Band ("Dither Band Width (screen pixels)", Float) = 120
        _Pixel ("Pixel Size (screen pixels)", Float) = 4
        _ViewSize ("Screen Size (pixels)", Vector) = (1920, 1080, 0, 0)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _Center;
                float _Radius;
                float _Band;
                float _Pixel;
                float4 _ViewSize;
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
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv; // the image covers the screen: 0 at the bottom left, 1 at the top right
                o.color = input.color;
                return o;
            }

            // 4x4 ordered dither threshold for a cell, between 0 and 1
            float Bayer4(float2 cell)
            {
                static const float matrix4[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
                int2 p = int2(fmod(cell, 4.0));
                return (matrix4[p.y * 4 + p.x] + 0.5) / 16.0;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float pixel = max(1.0, _Pixel);
                float2 cell = floor(input.uv * _ViewSize.xy / pixel);
                float dist = length((cell + 0.5) * pixel - _Center.xy);

                // 0 inside the light, rising to 1 across the band, then dark
                float dark = saturate((dist - _Radius) / max(1.0, _Band));
                float on = step(Bayer4(cell), dark);
                return half4(_Color.rgb, _Color.a * on * input.color.a);
            }
            ENDHLSL
        }
    }
}
