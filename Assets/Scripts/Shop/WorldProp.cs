using UnityEngine;

// A sprite standing on the ground facing the camera, like the characters: the root sits on the ground (so
// scaling it squashes from the feet) and the sprite is lifted so its bottom edge rests there. The shape of
// the things bought in the shop (mushrooms, the totem, Crimson Weed): Tools > Create Shop builds their
// prefabs with it, and swapping their sprites in play keeps them standing on the ground.
public static class WorldProp
{
    // The bare object: a camera-facing root with the sprite as a child, its bottom edge on the root
    public static GameObject Build(string name, Sprite sprite)
    {
        var root = new GameObject(name);
        root.AddComponent<LookAtCamera>().mode = LookAtCamera.Mode.CameraForward;

        var body = new GameObject("Sprite");
        body.transform.SetParent(root.transform, false);
        var renderer = body.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        if (sprite != null)
            body.transform.localPosition = Vector3.up * -sprite.bounds.min.y;
        return root;
    }

    // Swaps the sprite, keeping its bottom edge where the old one's was (so a sprite moved in the prefab
    // stays moved)
    public static void SetSprite(SpriteRenderer renderer, Sprite sprite)
    {
        Sprite old = renderer.sprite;
        if (old == sprite) return;
        renderer.sprite = sprite;
        if (old != null && sprite != null)
            renderer.transform.localPosition += Vector3.up * (old.bounds.min.y - sprite.bounds.min.y);
    }
}
