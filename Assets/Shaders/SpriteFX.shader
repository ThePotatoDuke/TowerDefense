// Unlit sprite shader with gameplay effects, based on URP's Sprite-Unlit-Default.
// All effects are driven per renderer by the SpriteFX component:
//  - Flash:    blend toward an HDR color (hit feedback, glows with bloom)
//  - Tint:     blend toward a color (e.g. castle low-health pulse)
//  - Dissolve: pixel-snapped noise burn-away with a glowing edge (enemy death)
//  - Shine:    diagonal light band sweeping across the sprite (pickups)
//  - Emission: brightness multiplier above 1 for bloom (projectiles)
Shader "TowerDefense/SpriteFX"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}

        [Header(Flash)]
        [HDR] _FlashColor ("Flash Color", Color) = (2, 2, 2, 1)
        _FlashAmount ("Flash Amount", Range(0, 1)) = 0

        [Header(Tint)]
        [HDR] _TintColor ("Tint Color", Color) = (1, 0, 0, 1)
        _TintAmount ("Tint Amount", Range(0, 1)) = 0

        [Header(Dissolve)]
        _Dissolve ("Dissolve", Range(0, 1)) = 0
        [HDR] _DissolveEdgeColor ("Edge Color", Color) = (4, 1.6, 0.4, 1)
        _DissolveEdgeWidth ("Edge Width", Range(0, 0.5)) = 0.12

        [Header(Shine)]
        [HDR] _ShineColor ("Shine Color", Color) = (2.5, 2.5, 2.5, 1)
        _ShineStrength ("Shine Strength", Range(0, 1)) = 0
        _ShineSpeed ("Shine Speed (px per sec)", Float) = 40
        _ShineSpacing ("Shine Spacing (px)", Float) = 60
        _ShineWidth ("Shine Width (px)", Float) = 4
        _ShineOffset ("Shine Offset (px)", Float) = 0

        [Header(Emission)]
        _Emission ("Emission", Float) = 1

        // Set from C# so effects snap to the sprite's texels
        [HideInInspector] _TexSize ("Texture Size (px)", Vector) = (64, 64, 0, 0)

        // Legacy sprite properties, kept for SpriteRenderer compatibility
        [HideInInspector] _Color ("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1, 1, 1, 1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _FlashColor;
                half _FlashAmount;
                half4 _TintColor;
                half _TintAmount;
                half _Dissolve;
                half4 _DissolveEdgeColor;
                half _DissolveEdgeWidth;
                half4 _ShineColor;
                half _ShineStrength;
                float _ShineSpeed;
                float _ShineSpacing;
                float _ShineWidth;
                float _ShineOffset;
                half _Emission;
                float4 _TexSize;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings Vert(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;

                // Texel coordinate, so every effect stays pixel-art crisp
                float2 px = floor(input.uv * _TexSize.xy);

                col.rgb = lerp(col.rgb, _TintColor.rgb, _TintAmount);
                col.rgb = lerp(col.rgb, _FlashColor.rgb, _FlashAmount);
                col.rgb *= _Emission;

                // Shine: diagonal band repeating every _ShineSpacing texels
                float band = frac((px.x + px.y + _ShineOffset - _Time.y * _ShineSpeed) / _ShineSpacing) * _ShineSpacing;
                col.rgb = lerp(col.rgb, _ShineColor.rgb, step(band, _ShineWidth) * _ShineStrength);

                // Dissolve: clumpy noise plus per-texel grain; pixels below the cutoff vanish,
                // a thin band above it glows
                float n = ValueNoise(px / 3.0) * 0.7 + Hash21(px) * 0.3;
                float cutoff = _Dissolve * (1.0 + _DissolveEdgeWidth);
                float removed = step(n, cutoff - _DissolveEdgeWidth);
                float edge = step(n, cutoff) * (1.0 - removed) * step(0.001, _Dissolve);
                col.rgb = lerp(col.rgb, _DissolveEdgeColor.rgb, edge);
                col.a *= 1.0 - removed;

                return col;
            }
            ENDHLSL
        }
    }
}
