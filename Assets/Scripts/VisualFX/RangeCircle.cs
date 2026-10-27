using UnityEngine;

// A dashed 1-pixel ring lying on the ground, drawn into a texture at the game's 16 pixels per unit so it
// stays as crisp as the rest of the pixel art at any radius. Coloured by the sprite's colour (e.g. green
// when a seed can be planted, red when not).
public class RangeCircle : MonoBehaviour
{
    private const float PixelsPerUnit = 16f;
    private const int DashLength = 3; // pixels on, then the same off
    // Below the sprites standing on the ground, like the GroundMarker
    private const int SortingOrder = -20;
    private const float Lift = 0.08f;

    private SpriteRenderer ring;
    private float radius = -1f;

    public static RangeCircle Create()
    {
        var circleObject = new GameObject("RangeCircle");
        var circle = circleObject.AddComponent<RangeCircle>();
        circle.ring = circleObject.AddComponent<SpriteRenderer>();
        circle.ring.sortingOrder = SortingOrder;
        return circle;
    }

    public void SetColor(Color color) => ring.color = color;

    // Centres it on a point on the ground
    public void Place(Vector3 groundPoint) => transform.position = groundPoint + Vector3.up * Lift;

    // Redraws the ring when the radius changes
    public void SetRadius(float newRadius)
    {
        if (Mathf.Approximately(newRadius, radius)) return;
        radius = newRadius;

        if (ring.sprite != null)
        {
            Destroy(ring.sprite.texture);
            Destroy(ring.sprite);
        }
        ring.sprite = Draw(radius);
    }

    private void LateUpdate()
    {
        // Flat on the ground, top edge away from the camera
        float cameraYaw = Camera.main != null ? Camera.main.transform.eulerAngles.y : 0f;
        transform.rotation = Quaternion.Euler(90f, cameraYaw, 0f);
    }

    private void OnDestroy()
    {
        if (ring != null && ring.sprite != null)
        {
            Destroy(ring.sprite.texture);
            Destroy(ring.sprite);
        }
    }

    private static Sprite Draw(float radius)
    {
        float pixelRadius = Mathf.Max(1f, radius * PixelsPerUnit);
        int size = Mathf.CeilToInt(pixelRadius * 2f) + 3;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "RangeCircle"
        };

        var pixels = new Color32[size * size];
        float center = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - center;
                float dy = y + 0.5f - center;
                if (Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - pixelRadius) > 0.5f) continue;

                // Dashes measured along the ring
                float along = (Mathf.Atan2(dy, dx) + Mathf.PI) * pixelRadius;
                if ((int)(along / DashLength) % 2 == 0)
                    pixels[y * size + x] = new Color32(255, 255, 255, 255);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }
}
