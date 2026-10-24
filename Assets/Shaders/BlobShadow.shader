// Soft, pixelated round shadow drawn on a flat quad under billboard sprites.
// Renders just before regular transparents so sprites always draw on top of it.
Shader "TowerDefense/BlobShadow"
{
    Properties
    {
        _Color ("Color", Color) = (0, 0, 0, 0.45)
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Radius ("Radius", Range(0, 1)) = 1
        _Softness ("Softness", Range(0.01, 1)) = 0.5
        _Pixels ("Pixel Resolution", Float) = 12
        _Steps ("Alpha Steps", Float) = 3
    }

    SubShader
    {
        Tags { "Queue" = "Transparent-50" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Offset -2, -2 // pull toward the camera so it never z-fights with the ground

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Alpha;
                half _Radius;
                half _Softness;
                float _Pixels;
                float _Steps;
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

            half4 Frag(Varyings input) : SV_Target
            {
                // Snap to a coarse grid and quantize alpha for a pixel-art look
                float2 uv = (floor(input.uv * _Pixels) + 0.5) / _Pixels;
                float d = length(uv * 2.0 - 1.0) / max(_Radius, 0.01);
                float a = 1.0 - smoothstep(1.0 - _Softness, 1.0, d);
                a = ceil(a * _Steps) / _Steps;
                return half4(_Color.rgb, _Color.a * _Alpha * a);
            }
            ENDHLSL
        }
    }
}
