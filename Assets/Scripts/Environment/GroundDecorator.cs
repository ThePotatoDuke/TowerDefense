using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Splines;

// Scatters decorations over the ground when the game starts: flower patches lying flat on the grass,
// and grass tufts that stand up facing the camera like the characters, sway when the player
// walks through them, and get mown down by sword swings. Keeps clear of the enemy path and of
// anything standing on the ground.
[RequireComponent(typeof(Renderer))]
public class GroundDecorator : MonoBehaviour
{
    public static GroundDecorator Instance { get; private set; }

    [Header("Sprites")]
    [SerializeField] private Material spriteMaterial;
    [SerializeField] private Sprite[] flowerSprites;
    [SerializeField] private Sprite[] grassSprites;

    [Header("Scatter")]
    [Tooltip("Flower patches per square unit of ground.")]
    [SerializeField] private float flowerDensity = 0.06f;
    [Tooltip("Grass tufts per square unit of ground, once fully grown.")]
    [SerializeField] private float grassDensity = 0.25f;
    [Tooltip("No decorations closer than this to the enemy path.")]
    [SerializeField] private float pathClearance = 1.3f;
    [Tooltip("How far flowers float above the ground, so they don't flicker into it.")]
    [SerializeField] private float flowerHeight = 0.02f;
    [Tooltip("Same seed, same layout every run.")]
    [SerializeField] private int seed = 12345;

    [Header("Grass Reaction")]
    [Tooltip("Tufts closer than this to the player bend away from them.")]
    [SerializeField] private float pushRadius = 0.8f;
    [SerializeField] private float maxBend = 30f;
    [Tooltip("How hard the grass springs back upright.")]
    [SerializeField] private float stiffness = 120f;
    [Tooltip("How quickly the wobble settles.")]
    [SerializeField] private float damping = 8f;

    [Header("Cutting")]
    [Tooltip("How close to the blade (seen from above) a tuft gets cut.")]
    [SerializeField] private float cutReach = 0.25f;
    [SerializeField] private int clippingsPerCut = 8;
    [SerializeField] private Color clippingColorA = new Color(0.39f, 0.67f, 0.25f, 1f);
    [SerializeField] private Color clippingColorB = new Color(0.23f, 0.49f, 0.31f, 1f);

    [Header("Growth")]
    [Tooltip("Share of the grass already grown when the game starts.")]
    [SerializeField, Range(0f, 1f)] private float startingGrowth = 0.5f;
    [Tooltip("Seconds over which the rest of the starting grass sprouts.")]
    [SerializeField] private float fillTime = 30f;
    [Tooltip("Average seconds before a cut tuft sprouts again at a new random spot.")]
    [SerializeField] private float regrowDelay = 15f;
    [Tooltip("Seconds a sprouting tuft takes to grow in.")]
    [SerializeField] private float growDuration = 0.6f;

    [Header("Grass Blades (currency)")]
    [SerializeField] private Sprite bladeSprite;
    [Tooltip("Collectible blades thrown out by each cut.")]
    [SerializeField] private int bladesPerCut = 3;
    [SerializeField] private GrassPickup.Settings bladeSettings = new GrassPickup.Settings();

    // Flat flowers draw before blob shadows (Transparent-50), so shadows fall on top of them
    private const int FlowerRenderQueue = 2940;
    private const int PathSamples = 200;
    private const float ClippingPixel = 1f / 16f;

    private class Tuft
    {
        public Transform root;
        public SpriteRenderer sprite;
        public float angle;
        public float velocity;
        public bool isCut;
        public float regrowTime;
    }

    private readonly List<Tuft> tufts = new List<Tuft>();
    private readonly List<Vector3> pathPoints = new List<Vector3>();
    private Transform container;
    private Bounds groundBounds;
    private Material flowerMaterial;
    private ParticleSystem clippings;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (spriteMaterial == null) return;

        container = new GameObject("GroundDecorations").transform;
        flowerMaterial = new Material(spriteMaterial) { name = "GroundFlowers (Instance)", renderQueue = FlowerRenderQueue };
        clippings = CreateClippings();

        SamplePath();

        Random.State previousState = Random.state;
        Random.InitState(seed);

        groundBounds = GetComponent<Renderer>().bounds;
        float area = groundBounds.size.x * groundBounds.size.z;

        if (flowerSprites != null && flowerSprites.Length > 0)
            Scatter(groundBounds, Mathf.RoundToInt(area * flowerDensity), SpawnFlower);
        if (grassSprites != null && grassSprites.Length > 0)
            Scatter(groundBounds, Mathf.RoundToInt(area * grassDensity), SpawnTuft);

        Random.state = previousState;

        // Start partly grown; the rest sprouts at random places over the first stretch of the game
        foreach (Tuft tuft in tufts)
        {
            if (Random.value < startingGrowth) continue;
            tuft.isCut = true;
            tuft.sprite.enabled = false;
            tuft.regrowTime = Time.time + Random.Range(0f, fillTime);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        if (container != null)
            Destroy(container.gameObject);
        if (flowerMaterial != null)
            Destroy(flowerMaterial);
    }

    private void Update()
    {
        Camera cam = Camera.main;
        if (Player.Instance == null || cam == null) return;

        float dt = Mathf.Min(Time.deltaTime, 1f / 30f);
        Vector3 playerPosition = Player.Instance.transform.position;
        Vector3 screenRight = cam.transform.right;

        foreach (Tuft tuft in tufts)
        {
            if (tuft.isCut)
            {
                if (Time.time >= tuft.regrowTime)
                    Regrow(tuft);
                continue;
            }

            Vector3 offset = tuft.root.position - playerPosition;
            offset.y = 0f;
            float distance = offset.magnitude;

            // Bend away from the player: tufts on their right lean right, and vice versa
            float target = 0f;
            if (distance < pushRadius)
            {
                float side = Vector3.Dot(offset, screenRight) >= 0f ? 1f : -1f;
                target = -side * maxBend * (1f - distance / pushRadius);
            }
            else if (Mathf.Abs(tuft.angle) < 0.01f && Mathf.Abs(tuft.velocity) < 0.01f)
            {
                continue; // at rest
            }

            // Damped spring, so the grass overshoots and wobbles back upright
            tuft.velocity += (stiffness * (target - tuft.angle) - damping * tuft.velocity) * dt;
            tuft.angle += tuft.velocity * dt;
        }
    }

    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // Face the camera like the characters, tilted by each tuft's bend
        Quaternion facing = cam.transform.rotation;
        foreach (Tuft tuft in tufts)
            tuft.root.rotation = facing * Quaternion.Euler(0f, 0f, tuft.angle);
    }

    // Closest uncut tuft within range on the ground plane, or null
    public GameObject FindStandingTuft(Vector3 position, float range)
    {
        Tuft closest = null;
        float closestSqr = range * range;

        foreach (Tuft tuft in tufts)
        {
            if (tuft.isCut) continue;

            float dx = tuft.root.position.x - position.x;
            float dz = tuft.root.position.z - position.z;
            float distanceSqr = dx * dx + dz * dz;
            if (distanceSqr < closestSqr)
            {
                closestSqr = distanceSqr;
                closest = tuft;
            }
        }
        return closest != null ? closest.root.gameObject : null;
    }

    // Cuts the standing tuft rooted at this exact spot, if any (the one a swing was aimed at)
    public void CutTuftAt(Vector3 rootPosition, Vector3 bladePosition)
    {
        foreach (Tuft tuft in tufts)
        {
            if (!tuft.isCut && (tuft.root.position - rootPosition).sqrMagnitude < 0.0001f)
            {
                Cut(tuft, bladePosition);
                return;
            }
        }
    }

    // Cuts every standing tuft the blade passes over. Height is ignored, so a swing at hand
    // height still mows the grass beneath it.
    public void CutGrass(Collider blade)
    {
        // ClosestPoint is unreliable on a disabled collider
        if (!blade.enabled || !blade.gameObject.activeInHierarchy) return;

        float bladeHeight = blade.bounds.center.y;
        float reachSqr = cutReach * cutReach;

        foreach (Tuft tuft in tufts)
        {
            if (tuft.isCut) continue;

            Vector3 point = tuft.root.position;
            point.y = bladeHeight;
            Vector3 closest = blade.ClosestPoint(point);
            float dx = closest.x - point.x;
            float dz = closest.z - point.z;
            if (dx * dx + dz * dz <= reachSqr)
                Cut(tuft, blade.transform.position);
        }
    }

    private void Cut(Tuft tuft, Vector3 bladePosition)
    {
        tuft.isCut = true;
        tuft.regrowTime = Time.time + regrowDelay * Random.Range(0.5f, 1.5f);
        tuft.angle = 0f;
        tuft.velocity = 0f;
        tuft.root.DOKill();
        tuft.root.localScale = Vector3.one;
        tuft.sprite.enabled = false;

        // Clippings and blades fly up and away from the blade
        Vector3 origin = tuft.sprite.bounds.center;
        Vector3 away = origin - bladePosition;
        away.y = 0f;
        away = away.sqrMagnitude > 0.0001f ? away.normalized : Vector3.up;

        ThrowBlades(origin, away, tuft.root.position.y);

        if (clippings == null) return;

        var emit = new ParticleSystem.EmitParams();
        for (int i = 0; i < clippingsPerCut; i++)
        {
            emit.position = origin + Random.insideUnitSphere * 0.15f;
            emit.velocity = (away * Random.Range(0.5f, 2f)) + Vector3.up * Random.Range(1.5f, 3.5f) + Random.insideUnitSphere * 0.5f;
            emit.startColor = Color.Lerp(clippingColorA, clippingColorB, Random.value);
            emit.startSize = ClippingPixel * Random.Range(1, 3); // 1 or 2 pixels
            emit.startLifetime = Random.Range(0.4f, 0.7f);
            clippings.Emit(emit, 1);
        }
    }

    private void ThrowBlades(Vector3 origin, Vector3 away, float groundY)
    {
        if (bladeSprite == null) return;

        for (int i = 0; i < bladesPerCut; i++)
        {
            var blade = new GameObject("GrassBlade");
            blade.transform.SetParent(container, false);
            blade.transform.position = origin + Random.insideUnitSphere * 0.1f;

            // A gentle pop, so they flutter down near the cut
            Vector3 velocity = away * Random.Range(0.3f, 1.2f)
                             + Vector3.up * Random.Range(1.5f, 2.5f)
                             + Vector3.ProjectOnPlane(Random.insideUnitSphere, Vector3.up) * 0.6f;

            blade.AddComponent<GrassPickup>().Launch(bladeSprite, spriteMaterial, bladeSettings, velocity, groundY);
        }
    }

    // Cut tufts sprout again somewhere new, so the field keeps changing as it's mown
    private void Regrow(Tuft tuft)
    {
        if (!TryFindGrowSpot(out Vector3 spot))
        {
            tuft.regrowTime = Time.time + 1f; // no room right now, try again shortly
            return;
        }

        tuft.root.position = spot;
        tuft.isCut = false;
        tuft.angle = 0f;
        tuft.velocity = 0f;
        tuft.sprite.sprite = grassSprites[Random.Range(0, grassSprites.Length)];
        tuft.sprite.flipX = Random.value < 0.5f;
        tuft.sprite.transform.localPosition = Vector3.up * -tuft.sprite.sprite.bounds.min.y;
        tuft.sprite.enabled = true;

        // Sprout: rise out of the ground, widen, and settle with a small overshoot.
        // Scale the root, which sits at the base, so the tuft grows up from its bottom edge
        Transform root = tuft.root;
        root.DOKill();
        root.localScale = new Vector3(0.4f, 0f, 1f);
        root.DOScale(Vector3.one, growDuration).SetEase(Ease.OutBack).SetLink(root.gameObject);
    }

    private bool TryFindGrowSpot(out Vector3 spot)
    {
        Vector3 playerPosition = Player.Instance != null ? Player.Instance.transform.position : Vector3.positiveInfinity;

        for (int attempt = 0; attempt < 10; attempt++)
        {
            var candidate = new Vector3(
                Random.Range(groundBounds.min.x, groundBounds.max.x),
                groundBounds.max.y,
                Random.Range(groundBounds.min.z, groundBounds.max.z));

            // Not right under the player, where it would be flattened the moment it appears
            float dx = candidate.x - playerPosition.x;
            float dz = candidate.z - playerPosition.z;
            if (dx * dx + dz * dz < pushRadius * pushRadius) continue;

            if (IsNearPath(candidate)) continue;
            if (TryGetGroundPoint(candidate, out spot)) return true;
        }

        spot = default;
        return false;
    }

    private void Scatter(Bounds bounds, int count, System.Action<Vector3> spawn)
    {
        for (int i = 0; i < count; i++)
        {
            var candidate = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.max.y,
                Random.Range(bounds.min.z, bounds.max.z));

            if (IsNearPath(candidate)) continue;
            if (!TryGetGroundPoint(candidate, out Vector3 groundPoint)) continue;

            spawn(groundPoint);
        }
    }

    private void SpawnFlower(Vector3 groundPoint)
    {
        var flower = new GameObject("Flower").transform;
        flower.SetParent(container, false);
        flower.position = groundPoint + Vector3.up * flowerHeight;

        // Lie flat with the top of the sprite pointing away from the camera, so it reads upright on screen
        float cameraYaw = Camera.main != null ? Camera.main.transform.eulerAngles.y : 0f;
        flower.rotation = Quaternion.Euler(90f, cameraYaw, 0f);

        var sprite = flower.gameObject.AddComponent<SpriteRenderer>();
        sprite.sprite = flowerSprites[Random.Range(0, flowerSprites.Length)];
        sprite.sharedMaterial = flowerMaterial;
        sprite.flipX = Random.value < 0.5f;
        sprite.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        sprite.receiveShadows = false;
    }

    private void SpawnTuft(Vector3 groundPoint)
    {
        // The root sits on the ground and is what rotates, so the tuft bends from its base
        var root = new GameObject("GrassTuft").transform;
        root.SetParent(container, false);
        root.position = groundPoint;

        var spriteObject = new GameObject("Sprite").transform;
        spriteObject.SetParent(root, false);

        var sprite = spriteObject.gameObject.AddComponent<SpriteRenderer>();
        sprite.sprite = grassSprites[Random.Range(0, grassSprites.Length)];
        sprite.sharedMaterial = spriteMaterial;
        sprite.flipX = Random.value < 0.5f;
        sprite.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        sprite.receiveShadows = false;

        // Lift the sprite so its bottom edge, not its middle, is at the root
        spriteObject.localPosition = Vector3.up * -sprite.sprite.bounds.min.y;

        tufts.Add(new Tuft { root = root, sprite = sprite });
    }

    // One shared world-space system for all grass clippings, emitted by hand on each cut
    private ParticleSystem CreateClippings()
    {
        FXSettings settings = FXSettings.Instance;
        if (settings == null || settings.DebrisMaterial == null) return null;

        var clippingObject = new GameObject("GrassClippings");
        clippingObject.transform.SetParent(container, false);

        var system = clippingObject.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = system.main;
        main.loop = true;
        main.playOnAwake = false;
        main.gravityModifier = 2.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 500;

        var emission = system.emission;
        emission.enabled = false;

        var shape = system.shape;
        shape.enabled = false;

        var sizeOverLifetime = system.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

        // Untextured billboard quads read as square pixels
        var particleRenderer = clippingObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = settings.DebrisMaterial;
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;

        system.Play();
        return system;
    }

    private void SamplePath()
    {
        SplineContainer path = FindAnyObjectByType<SplineContainer>();
        if (path == null) return;

        for (int i = 0; i <= PathSamples; i++)
            pathPoints.Add(path.EvaluatePosition(i / (float)PathSamples));
    }

    private bool IsNearPath(Vector3 point)
    {
        float clearanceSqr = pathClearance * pathClearance;
        foreach (Vector3 pathPoint in pathPoints)
        {
            float dx = point.x - pathPoint.x;
            float dz = point.z - pathPoint.z;
            if (dx * dx + dz * dz < clearanceSqr)
                return true;
        }
        return false;
    }

    // Only where this ground is the first thing hit from above, so nothing ends up inside
    // the castle, rocks or other props
    private bool TryGetGroundPoint(Vector3 point, out Vector3 groundPoint)
    {
        groundPoint = default;
        if (!Physics.Raycast(point + Vector3.up * 30f, Vector3.down, out RaycastHit hit, 100f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return false;
        if (hit.collider.gameObject != gameObject)
            return false;

        groundPoint = hit.point;
        return true;
    }
}
