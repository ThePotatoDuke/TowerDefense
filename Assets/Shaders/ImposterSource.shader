// Material the PixelImposter puts on a 3D model while its imposter camera renders it. Simple lighting
// (main light + ambient), and each pixel's distance from the camera stored in alpha: 0.5 at the near
// edge of the model's range up to 1 at the far edge (0 stays empty background). The imposter quad
// turns that back into real depth, so the pixel-art model sorts correctly against everything else.
Shader "TowerDefense/ImposterSource"
{
    Properties
    {
        [MainTexture] _BaseMap ("Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
            CBUFFER_END

            // Set globally by PixelImposter just before each imposter camera renders its own model
            float _ImposterNear;
            float _ImposterFar;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                float3 normal = normalize(input.normalWS);
                Light light = GetMainLight();
                half3 lighting = light.color * saturate(dot(normal, light.direction)) + SampleSH(normal);

                // The imposter camera sits at the main camera's position, so this is the distance the main view sees
                float distanceFromCamera = distance(input.positionWS, _WorldSpaceCameraPos);
                half depth01 = saturate((distanceFromCamera - _ImposterNear) / max(0.001, _ImposterFar - _ImposterNear));

                return half4(albedo.rgb * lighting, 0.5 + 0.5 * depth01);
            }
            ENDHLSL
        }
    }
}
