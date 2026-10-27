using UnityEngine;
using UnityEngine.UI;

// The one pixel scale for all pixel-art UI, so every art pixel is the same size on screen: 4 canvas units
// per pixel (4 screen pixels at 1080p). Boards, frames and icons are all sized through here.
public static class UIPixels
{
    public const float Scale = 4f;

    // Sizes the image to its sprite, each art pixel this many canvas units (the UI's pixel scale if not given)
    public static void Fit(Image image, float pixelSize = Scale)
    {
        if (image != null && image.sprite != null)
            image.rectTransform.sizeDelta = image.sprite.rect.size * pixelSize;
    }

    // Shows the sprite at its pixel size, or hides the image when there's none. Given a frame (the board an
    // icon sits on), its art pixels come out the same size as the frame's.
    public static void Show(Image image, Sprite sprite, Image frame = null)
    {
        if (image == null) return;
        image.sprite = sprite;
        image.enabled = sprite != null;
        Fit(image, frame != null ? PixelSize(frame) : Scale);
    }

    // Canvas units per art pixel of a pixel-art image as it's drawn: for a sliced or tiled board that's set
    // by its Pixels Per Unit Multiplier (whole numbers of units stay crisp, e.g. a 16 px/unit sprite on a
    // 100 reference canvas: 1.5625 = 4, 1.25 = 5, 1.0417 = 6, 0.78125 = 8), otherwise by how far it's stretched
    public static float PixelSize(Image image)
    {
        if (image == null || image.sprite == null) return Scale;

        if (image.type == Image.Type.Sliced || image.type == Image.Type.Tiled)
        {
            Canvas canvas = image.canvas;
            float referencePixelsPerUnit = canvas != null ? canvas.rootCanvas.referencePixelsPerUnit : 100f;
            return referencePixelsPerUnit / (image.sprite.pixelsPerUnit * Mathf.Max(0.01f, image.pixelsPerUnitMultiplier));
        }
        float width = image.sprite.rect.width;
        return width > 0f ? image.rectTransform.rect.width / width : Scale;
    }

    // A board that stretches to any size: sliced, its edges drawn at the UI's pixel scale
    public static void Slice(Image image, Canvas canvas)
    {
        Sprite sprite = image.sprite;
        if (sprite == null || sprite.border == Vector4.zero) return;
        float referencePixelsPerUnit = canvas != null ? canvas.rootCanvas.referencePixelsPerUnit : 100f;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = referencePixelsPerUnit / (sprite.pixelsPerUnit * Scale);
    }
}
