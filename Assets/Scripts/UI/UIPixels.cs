using UnityEngine;
using UnityEngine.UI;

// The one pixel scale for all pixel-art UI, so every art pixel is the same size on screen: 4 canvas units
// per pixel (4 screen pixels at 1080p). Boards, frames and icons are all sized through here.
public static class UIPixels
{
    public const float Scale = 4f;

    // Sizes the image to its sprite at the UI's pixel scale
    public static void Fit(Image image)
    {
        if (image != null && image.sprite != null)
            image.rectTransform.sizeDelta = image.sprite.rect.size * Scale;
    }

    // Shows the sprite at its pixel size, or hides the image when there's none
    public static void Show(Image image, Sprite sprite)
    {
        if (image == null) return;
        image.sprite = sprite;
        image.enabled = sprite != null;
        Fit(image);
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
