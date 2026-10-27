// The checkerboard of pixel diamonds behind the main menu and the shop, drawn by ScrollingBackground (which
// sets everything here). The UI image's uv is in tiles, one diamond and the gaps around it per tile, and is
// cut to whole art pixels so edges stay crisp. Reveal grows each diamond from nothing, one after another
// along Reveal Direction (0 = all gone, 1 = all full size); a diamond only ever covers its own cell, so they
// never overlap while growing. Lives in Resources so it's always in the build.
Shader "TowerDefense/UI Pixel Diamonds"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        _DiamondColor ("Diamond Color", Color) = (0.184, 0.341, 0.325, 1)
        _GapColor ("Gap Color", Color) = (0.157, 0.208, 0.251, 1)
        _Pixels ("Art Pixels Per Diamond", Float) = 32
        _UVRect ("Shown UV Rect (x, y, width, height)", Vector) = (0, 0, 1, 1)
        _RevealDirection ("Reveal Direction", Vector) = (1, 1, 0, 0)
        _Reveal ("Reveal", Range(0, 1)) = 1
        _RevealSpread ("Reveal Spread (how many grow at once)", Range(0.05, 1)) = 0.3

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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

            #include "UnityCG.cginc"

            fixed4 _DiamondColor;
            fixed4 _GapColor;
            float _Pixels;
            float4 _UVRect;
            float4 _RevealDirection;
            float _Reveal;
            float _RevealSpread;

            struct Attributes
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                // The middle of this art pixel, in tiles
                float pixels = max(_Pixels, 1);
                float2 tile = (floor(input.uv * pixels) + 0.5) / pixels;

                // Turned 45 degrees the diamonds become a grid of unit squares: the tile's own diamond is
                // the cell at (1, 0), the gaps round it the cells at its corners
                float2 turned = float2(tile.x + tile.y, tile.x - tile.y);
                float2 cell = floor(turned + 0.5);
                float2 offset = abs(turned - cell);
                float fromMiddle = max(offset.x, offset.y); // 0 in the middle, 0.5 at the edge
                bool isDiamond = fmod(abs(cell.x + cell.y), 2) > 0.5;

                // When this cell grows: by where its middle is on screen, along the reveal direction
                float2 middle = float2(cell.x + cell.y, cell.x - cell.y) * 0.5;
                float2 screen = (middle - _UVRect.xy) / max(abs(_UVRect.zw), 0.0001);
                float2 direction = _RevealDirection.xy;
                if (dot(direction, direction) < 0.0001) direction = float2(1, 1);
                direction = normalize(direction);
                float first = min(0, direction.x) + min(0, direction.y);
                float last = max(0, direction.x) + max(0, direction.y);
                float order = saturate((dot(screen, direction) - first) / max(last - first, 0.0001));
                float grow = saturate((_Reveal * (1 + _RevealSpread) - order) / _RevealSpread);
                grow = grow * grow * (3 - 2 * grow);

                if (grow < 0.999 && fromMiddle >= grow * 0.5)
                    discard;

                return (isDiamond ? _DiamondColor : _GapColor) * input.color;
            }
            ENDCG
        }
    }
}
