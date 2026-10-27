using UnityEngine;

// One Spore Mine mushroom, and (on its prefab, Prefabs/Shop/SporeMine, which ShopSettings points to) the
// numbers for the whole group: where they grow, how hard and wide their burst is. SporeMines plants them
// and sets them off.
public class SporeMine : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Sprite mushroomSprite;
    [SerializeField] private Sprite wiltedSprite;

    [Header("Group")]
    [Tooltip("Where the mushrooms grow, in order: x = how far back along the path from the castle, " +
             "y = how far to the side (negative for the other side).")]
    [SerializeField] private Vector2[] spots =
    {
        new(1.2f, 1.5f), new(1.2f, -1.5f), new(2.2f, 1.8f), new(2.2f, -1.8f), new(3.2f, 1.5f), new(3.2f, -1.5f)
    };

    [Header("Burst")]
    [SerializeField] private float damagePerMushroom = 20f;
    [Tooltip("Blast radius with one mushroom; each more adds Radius Per Mushroom.")]
    [SerializeField] private float radius = 2.5f;
    [SerializeField] private float radiusPerMushroom = 0.2f;
    [Tooltip("Seconds the mushrooms swell before bursting.")]
    [SerializeField] private float fuseTime = 0.3f;
    [SerializeField] private float shake = 0.35f;
    [SerializeField] private Color sporeColorA = new(0.62f, 0.85f, 0.35f);
    [SerializeField] private Color sporeColorB = new(0.85f, 0.55f, 0.85f);

    public SpriteRenderer Sprite => sprite != null ? sprite : sprite = GetComponentInChildren<SpriteRenderer>();
    public Sprite MushroomSprite => mushroomSprite;
    public Sprite WiltedSprite => wiltedSprite;
    public Vector2[] Spots => spots;
    public float DamagePerMushroom => damagePerMushroom;
    public float Radius => radius;
    public float RadiusPerMushroom => radiusPerMushroom;
    public float FuseTime => fuseTime;
    public float Shake => shake;
    public Color SporeColorA => sporeColorA;
    public Color SporeColorB => sporeColorB;
}
