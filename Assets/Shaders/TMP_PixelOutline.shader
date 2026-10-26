// TextMeshPro text with a square pixel outline, for blocky pixel fonts. TextMeshPro's own outline grows
// the letters evenly in every direction, which rounds their corners; this one reads the font atlas pixel
// by pixel (no smoothing) and grows the letters by whole atlas pixels in a square, so corners stay square.
// Works with the usual SDF font assets as well as bitmap ones: a pixel counts as part of a letter when it's
// past the halfway mark. The font asset needs at least as much atlas padding as the outline is wide, or
// bits of neighboring letters show around the edges.
// Use it through a material preset made with Assets > Create > TextMeshPro > Pixel Outline Material.
Shader "TowerDefense/TMP Pixel Outline"
{
    Properties
    {
        _MainTex ("Font Atlas", 2D) = "white" {}
        _FaceColor ("Face Color (times the text's color)", Color) = (1, 1, 1, 1)
        _OutlineColor ("Outline Color", Color) = (0.208, 0.188, 0.2, 1)
        // TextMeshPro reads _Padding to make each letter's quad big enough to hold the outline
        _Padding ("Outline Width (atlas pixels)", Range(0, 12)) = 3

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        _ClipRect ("Clip Rect", Vector) = (-32767, -32767, 32767, 32767)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            Texture2D _MainTex;
            SamplerState sampler_point_clamp; // whole atlas pixels, never blended
            float4 _MainTex_TexelSize;
            fixed4 _FaceColor;
            fixed4 _OutlineColor;
            float _Padding;
            float4 _ClipRect;

            struct Attributes
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float4 texcoord0 : TEXCOORD0;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.worldPosition = input.vertex;
                o.vertex = UnityObjectToClipPos(input.vertex);
                o.color = input.color;
                o.uv = input.texcoord0.xy;
                return o;
            }

            // 1 inside a letter, 0 outside
            float Inside(float2 uv)
            {
                return step(0.5, _MainTex.SampleLevel(sampler_point_clamp, uv, 0).a);
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                float face = Inside(input.uv);

                // Any letter pixel within the outline width, in a square around this one
                float outline = face;
                int width = (int)round(_Padding);
                for (int y = -width; y <= width && outline < 1.0; y++)
                {
                    for (int x = -width; x <= width; x++)
                    {
                        outline = max(outline, Inside(input.uv + float2(x, y) * _MainTex_TexelSize.xy));
                    }
                }

                fixed4 faceColor = _FaceColor * input.color;
                fixed4 color = face > 0.5 ? faceColor : fixed4(_OutlineColor.rgb, _OutlineColor.a * outline * input.color.a);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
