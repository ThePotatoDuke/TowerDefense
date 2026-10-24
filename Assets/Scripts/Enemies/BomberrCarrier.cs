using DG.Tweening;
using UnityEngine;

// One of the little bomberrs: a sprite that faces the camera, plays its frames and turns to face
// the way it's moving (the art faces left). Carries the bomb until told to flee.
public class BomberrCarrier : MonoBehaviour
{
    private SpriteRenderer sprite;
    private SpriteFX spriteFX;
    private Sprite[] frames;
    private float framesPerSecond;
    private float frameClock;
    private Vector3 lastPosition;

    public void Setup(Sprite[] carryFrames, float fps, Material material)
    {
        sprite = gameObject.AddComponent<SpriteRenderer>();
        sprite.sharedMaterial = material;
        sprite.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        sprite.receiveShadows = false;

        spriteFX = gameObject.GetOrAddComponent<SpriteFX>();

        frames = carryFrames;
        framesPerSecond = fps;
        frameClock = Random.value * 10f; // out of step with the others
        lastPosition = transform.position;
        ShowFrame();
    }

    public void Flash()
    {
        if (spriteFX != null && spriteFX.IsReady)
            spriteFX.Flash();
    }

    // Let go of the bomb and run off in this direction, fading out as it goes
    public void Flee(Sprite[] runFrames, float fps, Vector3 direction, float speed, float duration)
    {
        transform.SetParent(null, true);
        if (runFrames != null && runFrames.Length > 0)
        {
            frames = runFrames;
            framesPerSecond = fps;
        }

        transform.DOMove(transform.position + direction * speed * duration, duration)
                 .SetEase(Ease.OutQuad)
                 .SetLink(gameObject);
        sprite.DOFade(0f, duration * 0.6f)
              .SetDelay(duration * 0.4f)
              .SetLink(gameObject)
              .OnComplete(() => Destroy(gameObject));
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0) return;

        frameClock += Time.deltaTime;
        ShowFrame();
    }

    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        transform.rotation = cam.transform.rotation;

        // Face the way we're moving across the screen; the art faces left
        float sideways = Vector3.Dot(transform.position - lastPosition, cam.transform.right);
        if (Mathf.Abs(sideways) > 0.0001f)
            sprite.flipX = sideways > 0f;
        lastPosition = transform.position;
    }

    private void ShowFrame()
    {
        if (frames == null || frames.Length == 0) return;
        sprite.sprite = frames[(int)(frameClock * framesPerSecond) % frames.Length];
    }
}
