// Displays a low-res render of a 3D model as crisp pixel art on a camera-facing quad,
// with a 1-texel outline around its silhouette. Fed by the PixelImposter component.
Shader "TowerDefense/PixelImposter"
{
    Properties
    {
        _MainTex ("Imposter Texture", 2D) = "black" {}
        _OutlineColor ("Outline Color", Color) = (0.08, 0.06, 0.1, 1)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        [HideInInspector] _TexSize ("Texture Size (px)", Vector) = (32, 32, 0, 0)
    }

    SubShader
    {
        Tags { "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Cull Off
        ZWrite On

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                half _Cutoff;
                float4 _TexSize;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                return o;
            }

            half SampleAlpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 texel = 1.0 / _TexSize.xy;
                float2 uv = (floor(input.uv * _TexSize.xy) + 0.5) * texel; // texel center

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                if (col.a >= _Cutoff)
                    return half4(col.rgb, 1.0);

                // Empty texel: outline it if any direct neighbour is part of the model
                half neighbour = max(max(SampleAlpha(uv + float2(texel.x, 0)), SampleAlpha(uv - float2(texel.x, 0))),
                                     max(SampleAlpha(uv + float2(0, texel.y)), SampleAlpha(uv - float2(0, texel.y))));
                clip(neighbour - _Cutoff);
                return half4(_OutlineColor.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
