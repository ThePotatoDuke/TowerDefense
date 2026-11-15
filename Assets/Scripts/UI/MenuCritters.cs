using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The game's enemies wandering across the main menu: every few seconds one of the critter prefabs (or a
// Konga chain, or a swarm of Mini Slimes) comes in from off one side, wanders to a few spots and leaves
// off a side (a line of Checker Slimes leapfrogs across). Ground critters (walkers, slimes, Tibbits,
// Mollies, spike balls) walk along the bottom edge of the screen;
// flyers go anywhere above it. Tibbits ride the others. They draw over the sign and buttons, the lower on screen the
// nearer. How each moves is on its prefab (Prefabs/Menu/Critters).
public class MenuCritters : MonoBehaviour
{
    [SerializeField] private List<MenuCritter> critters = new();

    [Tooltip("Art pixels per world unit times the menu's pixel scale: turns the game's speeds and heights " +
             "into menu ones (16 x 5).")]
    [SerializeField] private float canvasUnitsPerWorldUnit = 80f;

    [Header("Where")]
    [Tooltip("Where ground critters' feet are, up from the bottom of the screen (canvas units; 0 = the very bottom).")]
    [SerializeField] private float groundLine;
    [Tooltip("Lowest flyers go, up from the bottom of the screen (canvas units).")]
    [SerializeField] private float flyBottom = 200f;
    [Tooltip("Highest flyers go, as a share of the screen's height from the bottom.")]
    [SerializeField, Range(0f, 1f)] private float flyTop = 0.85f;
    [Tooltip("How far beyond the screen's edge they come in from and leave to.")]
    [SerializeField] private float offscreenMargin = 200f;

    [Header("When")]
    [SerializeField] private float startDelay = 2.5f;
    [Tooltip("Seconds between arrivals, picked between the two.")]
    [SerializeField] private Vector2 spawnGap = new(1.5f, 4f);
    [SerializeField] private int maxOnScreen = 10;
    [Tooltip("Spots each visits before leaving, picked between the two.")]
    [SerializeField] private Vector2Int stops = new(1, 3);

    private readonly List<RectTransform> alive = new();
    private RectTransform area;

    // Screen shake: the whole menu (this layer and everything beside it) jolted about
    private float shakeStrength, shakeTime, shakeLeft, nextJolt;
    private readonly Dictionary<Transform, Vector3> shaken = new(); // where each was put, with its offset
    private Vector3 offset;

    private void Start()
    {
        area = (RectTransform)transform;
        if (critters.Count > 0)
            StartCoroutine(Spawning());
    }

    private void Update()
    {
        // Lower on screen is nearer, so drawn on top (riders go with what they ride)
        alive.RemoveAll(critter => critter == null);
        alive.Sort((a, b) => b.anchoredPosition.y.CompareTo(a.anchoredPosition.y));
        int index = 0;
        foreach (RectTransform critter in alive)
            if (critter.parent == transform)
                critter.SetSiblingIndex(index++);
    }

    // Jolts the menu about for a moment, fading out (a spike ball landing)
    public void Shake(float strength, float duration)
    {
        if (strength <= 0f || duration <= 0f) return;
        shakeStrength = Mathf.Max(strength, shakeLeft > 0f ? shakeStrength : 0f);
        shakeTime = shakeLeft = Mathf.Max(duration, shakeLeft);
        nextJolt = 0f;
    }

    // After everything else has moved this frame: takes last frame's offset back off whatever hasn't been
    // moved since (a tween or the intro may have), then puts a new one on. Offsets are whole menu pixels.
    private void LateUpdate()
    {
        if (shakeLeft <= 0f && shaken.Count == 0) return;
        Transform canvas = transform.parent;
        if (canvas == null) return;

        shakeLeft -= Time.deltaTime;
        nextJolt -= Time.deltaTime;
        Vector3 previous = offset;
        if (shakeLeft <= 0f)
            offset = Vector3.zero;
        else if (nextJolt <= 0f)
        {
            nextJolt = 0.04f;
            float pixel = Mathf.Max(1f, canvasUnitsPerWorldUnit / 16f);
            Vector2 jolt = Random.insideUnitCircle * (shakeStrength * shakeLeft / shakeTime);
            jolt.x *= 1.5f; // mostly sideways
            offset = new Vector3(Mathf.Round(jolt.x / pixel) * pixel, Mathf.Round(jolt.y / pixel) * pixel, 0f);
        }

        foreach (Transform child in canvas)
        {
            Vector3 position = child.localPosition;
            if (shaken.TryGetValue(child, out Vector3 placed) && placed == position)
                position -= previous;
            child.localPosition = position + offset;
            shaken[child] = child.localPosition;
        }
        if (offset == Vector3.zero)
            shaken.Clear();
    }

    // Whether a point is on screen, give or take a margin (inward when positive)
    public bool OnScreen(Vector2 point, float margin)
    {
        Vector2 half = area.rect.size * 0.5f;
        return Mathf.Abs(point.x) < half.x - margin && Mathf.Abs(point.y) < half.y - margin;
    }

    // The nearest spot to this one on the ground, on screen
    public Vector2 OnGroundNear(Vector2 point)
    {
        Vector2 half = area.rect.size * 0.5f;
        point.x = Mathf.Clamp(point.x, -half.x + 60f, half.x - 60f);
        point.y = -half.y + groundLine;
        return point;
    }

    private IEnumerator Spawning()
    {
        yield return new WaitForSeconds(startDelay);
        while (true)
        {
            MenuCritter prefab = Pick();
            int count = prefab != null ? prefab.RollGroupSize() : 0;
            if (prefab != null && alive.Count + count <= maxOnScreen)
                SpawnGroup(prefab, count);
            yield return new WaitForSeconds(Random.Range(spawnGap.x, spawnGap.y));
        }
    }

    private MenuCritter Pick()
    {
        float total = 0f;
        foreach (MenuCritter critter in critters)
            if (critter != null) total += critter.Weight;
        float roll = Random.value * total;
        foreach (MenuCritter critter in critters)
        {
            if (critter == null || critter.Weight <= 0f) continue;
            roll -= critter.Weight;
            if (roll <= 0f) return critter;
        }
        return null;
    }

    // One path for the whole group; each follows a little behind the one before (a swarm a bit scattered)
    private void SpawnGroup(MenuCritter prefab, int count)
    {
        Vector2[] path = MakePath(prefab);
        if (prefab.Moves == MenuCritter.Movement.CheckerHop)
        {
            StartCoroutine(CheckerHop(prefab, count, path));
            return;
        }
        for (int i = 0; i < count; i++)
        {
            Vector2[] own = path;
            if (count > 1 && prefab.Moves == MenuCritter.Movement.Hop)
            {
                Vector2 jitter = new Vector2(Random.Range(-30f, 30f), prefab.OnGround ? 0f : Random.Range(-30f, 30f));
                own = System.Array.ConvertAll(path, point => point + jitter);
            }
            StartCoroutine(SpawnLater(prefab, own, prefab.GroupDelay * i));
        }
    }

    // A black and white line (either colour first) lined up behind the start, heading in; the one at the
    // back jumps over the rest to land a space ahead of the front, on the beat, straight across (no stops,
    // it never turns round) until the whole line is off the far side
    private IEnumerator CheckerHop(MenuCritter prefab, int count, Vector2[] path)
    {
        float spacing = prefab.Spacing * canvasUnitsPerWorldUnit;
        Vector2 start = path[0];
        float heading = -Mathf.Sign(start.x);
        float goal = -start.x;
        bool otherFirst = Random.value < 0.5f;
        var line = new List<MenuCritter>();
        for (int i = 0; i < count; i++)
        {
            MenuCritter critter = Instantiate(prefab, area);
            alive.Add((RectTransform)critter.transform);
            critter.BeginInLine(start - new Vector2(heading * spacing * i, 0f), otherFirst == (i % 2 == 0), heading,
                                canvasUnitsPerWorldUnit, this);
            line.Add(critter);
        }

        while (true)
        {
            line.RemoveAll(critter => critter == null);
            if (line.Count == 0) yield break;

            // Front and back by where they'll land; done once even the back is off the far side
            MenuCritter front = line[0], back = line[0];
            foreach (MenuCritter critter in line)
            {
                if (critter.LandingX * heading > front.LandingX * heading) front = critter;
                if (critter.LandingX * heading < back.LandingX * heading) back = critter;
            }
            if (back.LandingX * heading >= goal * heading) break;

            // The next takes off on the beat, before this one lands
            back.CheckerHop(front.LandingX + heading * spacing, line.Count - 1);
            yield return new WaitForSeconds(prefab.CheckerHopGap);
        }

        yield return new WaitForSeconds(prefab.CheckerHopGap * line.Count); // the last jumps land
        foreach (MenuCritter critter in line)
            if (critter != null)
                critter.Remove();
    }

    private IEnumerator SpawnLater(MenuCritter prefab, Vector2[] path, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        MenuCritter critter = Instantiate(prefab, area);
        alive.Add((RectTransform)critter.transform);
        critter.Begin(path, canvasUnitsPerWorldUnit, this);
    }

    // In from off one side, a few spots on screen, out off a side, at heights it can be at
    private Vector2[] MakePath(MenuCritter prefab)
    {
        Vector2 half = area.rect.size * 0.5f;
        float inX = half.x - 150f;
        var path = new List<Vector2>();
        float side = Random.value < 0.5f ? -1f : 1f;

        path.Add(new Vector2(side * (half.x + offscreenMargin), RandomHeight(prefab)));
        int count = Random.Range(stops.x, stops.y + 1);
        for (int i = 0; i < count; i++)
            path.Add(new Vector2(Random.Range(-inX, inX), RandomHeight(prefab)));
        float exitSide = Random.value < 0.7f ? -side : side; // usually across, sometimes back
        path.Add(new Vector2(exitSide * (half.x + offscreenMargin), RandomHeight(prefab)));
        return path.ToArray();
    }

    private float RandomHeight(MenuCritter prefab)
    {
        float bottom = -area.rect.height * 0.5f;
        return prefab.OnGround
            ? bottom + groundLine
            : bottom + Random.Range(flyBottom, Mathf.Max(flyBottom, area.rect.height * flyTop));
    }
}
