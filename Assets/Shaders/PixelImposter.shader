// Displays a low-res render of a 3D model as crisp pixel art on a camera-facing quad,
// with a 1-texel outline around its silhouette. Fed by the PixelImposter component.
// The render stores each pixel's distance from the camera in alpha (see ImposterSource.shader); this
// turns it back into real depth, so the model sorts against the ground and other objects per pixel
// instead of at the quad's flat depth.
Shader "TowerDefense/PixelImposter"
{
    Properties
    {
        _MainTex ("Imposter Texture", 2D) = "black" {}
        _OutlineColor ("Outline Color", Color) = (0.08, 0.06, 0.1, 1)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        [HideInInspector] _TexSize ("Texture Size (px)", Vector) = (32, 32, 0, 0)
        [HideInInspector] _ImposterNear ("Near Distance", Float) = 0
        [HideInInspector] _ImposterFar ("Far Distance", Float) = 1
        [HideInInspector] _WriteDepth ("Write Model Depth", Float) = 1
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
                float _ImposterNear;
                float _ImposterFar;
                float _WriteDepth;
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
                float3 positionWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct FragOutput
            {
                half4 color : SV_Target;
                float depth : SV_Depth;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.uv = input.uv;
                return o;
            }

            half SampleAlpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            // Depth of the model's surface seen through this pixel, from the distance stored in alpha
            float ModelDepth(Varyings input, half alpha)
            {
                if (_WriteDepth < 0.5)
                    return input.positionCS.z;

                float distance01 = saturate((alpha - 0.5) * 2.0);
                float distanceFromCamera = lerp(_ImposterNear, _ImposterFar, distance01) - 0.03; // nudge to avoid z-fighting
                float3 viewRay = normalize(input.positionWS - _WorldSpaceCameraPos);
                float4 surfaceCS = TransformWorldToHClip(_WorldSpaceCameraPos + viewRay * distanceFromCamera);
                return surfaceCS.z / surfaceCS.w;
            }

            FragOutput Frag(Varyings input)
            {
                float2 texel = 1.0 / _TexSize.xy;
                float2 uv = (floor(input.uv * _TexSize.xy) + 0.5) * texel; // texel center

                FragOutput o;
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                if (col.a >= _Cutoff)
                {
                    o.color = half4(col.rgb, 1.0);
                    o.depth = ModelDepth(input, col.a);
                    return o;
                }

                // Empty texel: outline it if any direct neighbour is part of the model, at that neighbour's depth
                half neighbour = max(max(SampleAlpha(uv + float2(texel.x, 0)), SampleAlpha(uv - float2(texel.x, 0))),
                                     max(SampleAlpha(uv + float2(0, texel.y)), SampleAlpha(uv - float2(0, texel.y))));
                clip(neighbour - _Cutoff);
                o.color = half4(_OutlineColor.rgb, 1.0);
                o.depth = ModelDepth(input, neighbour);
                return o;
            }
            ENDHLSL
        }
    }
}
