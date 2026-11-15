using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

// One of the enemies wandering the main menu (a prefab in Prefabs/Menu/Critters, spawned by MenuCritters):
// follows the path it's given, moving the way the enemy does in the game, playing its frames and turning
// to face where it's going, then removes itself. Walkers walk; slimes hop with squash and stretch; bats and
// Frills weave and bob through the air; Mollies travel as a moving mound of dirt and peek up out of it at
// each stop; Tibbits hitch rides: they jump onto a critter in reach that's heading their way (the biggest
// one first, never one they've ridden before), get off when it turns away, is about to leave the screen or
// they've got where they wanted, and otherwise skip along on their own. As in the game, a Molly can be
// ridden only while she's up, whichever way the Tibbit is going, and drops it when she ducks. Spike balls
// drop from the sky as in the game: a marker grows on the ground, the ball falls onto it and shakes the
// screen, then
// rolls from stop to stop, its frames turning with the distance rolled. Checker Slimes come as a black and
// white line that leapfrogs along (MenuCritters leads it: the back one jumps over the rest to the front).
// Its numbers start as the enemy's own (Tools > Set Up Main Menu Critters) and can be changed here.
public class MenuCritter : MonoBehaviour
{
    public enum Movement { Walk, Hop, Fly, Dig, Skip, Roll, CheckerHop }
    public enum HopPattern { Regular, ShortShortLong, BackAndForward }
    private enum HopType { Short, Long, Back }

    private static readonly List<MenuCritter> all = new();

    [SerializeField] private Movement movement;
    [Tooltip("The sprite, raised off the ground for hops and flight; its pivot is at its feet.")]
    [SerializeField] private RectTransform body;
    [SerializeField] private Image image;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 8f;
    [Tooltip("Shown while in the air instead of the frames (a Tibbit's jump), if set.")]
    [SerializeField] private Sprite airSprite;
    [Tooltip("Drawn facing left, so it turns round to go right (otherwise the other way).")]
    [SerializeField] private bool facesLeft;
    [Tooltip("World units a second, as in the game.")]
    [SerializeField] private float speed = 2f;

    [Header("Hops (slimes, Tibbits)")]
    [SerializeField] private float hopHeight = 1f;
    [SerializeField] private float hopDuration = 0.3f;
    [SerializeField] private float hopInterval = 0.1f;
    [SerializeField] private float hopDistance = 1f;
    [SerializeField] private float squash = 0.8f;
    [SerializeField] private float stretch = 1.2f;

    [Header("Hop Pattern (slimes)")]
    [Tooltip("Regular: even hops. Short Short Long: two hops then a big one and a pause (Rhythm Slime). " +
             "Back And Forward: now and then hops back before going on (Trickster Slime).")]
    [SerializeField] private HopPattern hopPattern;
    [Tooltip("A long hop goes this many times as far, as high and for as long as a short one.")]
    [SerializeField] private float longHopDistance = 2f;
    [SerializeField] private float longHopHeight = 1.5f;
    [SerializeField] private float longHopDuration = 1.3f;
    [Tooltip("Short Short Long: seconds of pause after the long hop.")]
    [SerializeField] private float longHopWait = 0.5f;
    [Tooltip("Back And Forward: chance of a hop back after a forward one.")]
    [SerializeField, Range(0f, 1f)] private float backChance = 0.5f;
    [Tooltip("Back And Forward: chance a forward hop is a long one.")]
    [SerializeField, Range(0f, 1f)] private float longForwardChance = 0.5f;
    [Tooltip("Back And Forward: hops back before a long one is allowed, and before one is forced.")]
    [SerializeField] private int minBackForward = 1;
    [SerializeField] private int maxBackForward = 3;

    [Header("Flying (bats, Frills)")]
    [SerializeField] private float flyHeight = 0.6f;
    [SerializeField] private float waveAmplitude;
    [SerializeField] private float waveLength = 4f;
    [SerializeField] private float bobHeight;
    [SerializeField] private float bobsPerSecond = 1f;

    [Header("Digging (Mollies)")]
    [Tooltip("The moving mound of dirt, looping while it travels underground.")]
    [SerializeField] private Sprite[] digFrames;
    [SerializeField] private float digFramesPerSecond = 10f;
    [Tooltip("Coming up out of the mound (played backwards to duck down).")]
    [SerializeField] private Sprite[] peekFrames;
    [SerializeField] private float peekFramesPerSecond = 12f;
    [Tooltip("Seconds it stays up at each stop, picked between the two.")]
    [SerializeField] private Vector2 idleTime = new(1.5f, 2.5f);

    [Header("Riding (Tibbits)")]
    [SerializeField] private bool rides;
    [Tooltip("Longest it rides one critter before looking for another, picked between the two.")]
    [SerializeField] private Vector2 rideTime = new(2.5f, 4f);
    [Tooltip("How far it can jump to a critter, in world units.")]
    [SerializeField] private float jumpRange;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float jumpSpeed = 7f;
    [SerializeField] private Vector2 jumpTime = new(0.35f, 0.8f);
    [Tooltip("Seconds between looks for something to ride.")]
    [SerializeField] private float searchInterval = 0.4f;
    [Tooltip("Seconds on the ground after getting off before it takes another ride.")]
    [SerializeField] private float rideCooldown = 1.5f;
    [Tooltip("How closely a critter has to be heading its way to be worth riding (1 = exactly, 0 = sideways).")]
    [SerializeField, Range(0f, 1f)] private float sameWay = 0.5f;
    [Tooltip("Seconds its ride can head the wrong way before it gets off (it turns at its stops).")]
    [SerializeField] private float wrongWayGrace = 0.3f;

    [Header("Dropping And Rolling (spike balls)")]
    [Tooltip("The ground marker's spinning ring and its middle dot.")]
    [SerializeField] private Sprite markerOuter;
    [SerializeField] private Sprite markerMiddle;
    [Tooltip("Seconds the marker shows before the ball lands on it.")]
    [SerializeField] private float dropWarningTime = 1.2f;
    [SerializeField] private float markerPopTime = 0.5f;
    [Tooltip("Degrees per second the marker's ring turns.")]
    [SerializeField] private float markerSpin = 120f;
    [Tooltip("Seconds the fall takes (the end of the warning is when it lands).")]
    [SerializeField] private float dropFallTime = 0.45f;
    [Tooltip("How far the screen shakes when it lands (canvas units) and for how long.")]
    [SerializeField] private float landShake = 15f;
    [SerializeField] private float landShakeTime = 0.25f;
    [Tooltip("World units it rolls for each frame, so the frames turn with how far it goes.")]
    [SerializeField] private float rollFrameDistance = 0.2f;

    [Header("Checker Hop (Checker Slimes)")]
    [Tooltip("The other colour's frames: every other one in the line plays these.")]
    [SerializeField] private Sprite[] altFrames;
    [Tooltip("Height of a jump over no one, in world units, and the extra for each one it jumps over.")]
    [SerializeField, FormerlySerializedAs("leapHeight")] private float checkerHopHeight = 0.6f;
    [SerializeField, FormerlySerializedAs("leapHeightPerSlime")] private float checkerHopHeightPerSlime = 0.6f;
    [Tooltip("Seconds between one taking off and the next (sets the line's speed).")]
    [SerializeField, FormerlySerializedAs("leapGap")] private float checkerHopGap = 0.6f;
    [Tooltip("How many times faster than the line a jumper flies (2 = like the top of a rolling wheel).")]
    [SerializeField, FormerlySerializedAs("leapAirSpeed")] private float checkerHopAirSpeed = 3f;
    [Tooltip("Seconds it takes to squash down on landing, then to spring back (with a little wobble).")]
    [SerializeField] private float checkerSquashTime = 0.1f;
    [SerializeField] private float checkerSpringBackTime = 0.32f;

    [Header("Spawning")]
    [Tooltip("How many come together (a Konga chain, a Mini Slime swarm).")]
    [SerializeField] private int groupSize = 1;
    [Tooltip("If more than Group Size, how many come is picked between the two each time.")]
    [SerializeField] private int maxGroupSize;
    [Tooltip("World units between the ones in a group.")]
    [SerializeField] private float groupSpacing = 0.9f;
    [Tooltip("How likely it is to be picked, compared to the others.")]
    [SerializeField] private float weight = 1f;

    public Movement Moves => movement;
    public bool OnGround => movement != Movement.Fly;
    // How many come this time
    public int RollGroupSize()
    {
        int least = Mathf.Max(1, groupSize);
        int most = Mathf.Max(least, maxGroupSize);
        if (movement != Movement.CheckerHop)
            return Random.Range(least, most + 1);
        // Checker Hop lines are always even, so the checker holds as they turn over
        least = Mathf.Max(2, least + least % 2);
        most = Mathf.Max(least, most - most % 2);
        return Random.Range(least / 2, most / 2 + 1) * 2;
    }
    public float Spacing => groupSpacing;
    public float CheckerHopGap => checkerHopGap;
    public float GroupDelay => groupSpacing / Mathf.Max(0.1f, speed);
    public float Weight => frames != null && frames.Length > 0 ? weight : 0f;

    private RectTransform root;
    private MenuCritters owner;
    private float unitsToCanvas = 80f;
    private Sprite[] playing;
    private float playingFps;
    private float clock;
    private bool turned;
    private Tween turn;
    private MenuCritter rider;   // a Tibbit on its back
    private MenuCritter carrier; // what this Tibbit is riding
    private Vector2 velocity;    // smoothed, so a slime between hops still counts as going somewhere
    private Vector2 lastPosition;
    private Vector2[] tibbitPath;
    private int tibbitNext;
    private readonly HashSet<MenuCritter> ridden = new(); // a Tibbit never rides the same one twice
    private bool up; // a Molly out of the ground (rideable)
    private HopType lastHop = HopType.Short;
    private int shortHops;      // Short Short Long: short ones since the last long one
    private int backHops;       // Back And Forward: hops back since the last long one
    private int rollFrame;      // spike balls: the frame it's on, and the one this roll started on
    private int rollStart;

    // Something a Tibbit can jump on: anything but another Tibbit or a Molly that's underground, with no
    // rider yet
    private bool CanCarry => this != null && root != null && rider == null && carrier == null
                             && movement != Movement.Skip && movement != Movement.Roll
                             && (movement != Movement.Dig || up);

    // A Molly stands still while she's up: worth riding whichever way she isn't going
    private bool StandsStill => movement == Movement.Dig;

    private float Size => body != null ? body.rect.width * body.rect.height : 0f;

    private void OnEnable() => all.Add(this);
    private void OnDisable() => all.Remove(this);

    private void Update()
    {
        if (root == null || carrier != null || Time.deltaTime <= 0f) return;
        Vector2 position = root.anchoredPosition;
        Vector2 moved = (position - lastPosition) / Time.deltaTime;
        lastPosition = position;
        velocity = Vector2.Lerp(velocity, moved, 1f - Mathf.Exp(-4f * Time.deltaTime));
    }

    // Sets off along the path (canvas positions), then removes itself
    public void Begin(Vector2[] path, float canvasUnitsPerWorldUnit, MenuCritters spawner)
    {
        root = (RectTransform)transform;
        owner = spawner;
        unitsToCanvas = canvasUnitsPerWorldUnit;
        root.anchoredPosition = lastPosition = path[0];
        Play(frames, framesPerSecond);
        StartCoroutine(Run(path));
    }

    // --- Checker Slimes (led by MenuCritters) ---

    // Where it stands or will land
    public float LandingX { get; private set; }
    private Tween landing;

    // Stands in its place in a leapfrog line, in one colour or the other, facing the way the line goes (it
    // never turns round), waiting to be told to jump
    public void BeginInLine(Vector2 position, bool otherColour, float heading, float canvasUnitsPerWorldUnit,
                            MenuCritters spawner)
    {
        root = (RectTransform)transform;
        owner = spawner;
        unitsToCanvas = canvasUnitsPerWorldUnit;
        root.anchoredPosition = lastPosition = position;
        LandingX = position.x;
        if (otherColour && altFrames != null && altFrames.Length > 0)
            frames = altFrames;
        turned = heading > 0f == facesLeft;
        body.localRotation = Quaternion.Euler(0f, turned ? 180f : 0f, 0f);
        Play(frames, framesPerSecond);
        StartCoroutine(Idle());
    }

    private IEnumerator Idle()
    {
        while (true)
        {
            Animate(Time.deltaTime);
            yield return null;
        }
    }

    // Jumps over this many others to x on the ground, flying faster than the line moves
    public void CheckerHop(float x, int over)
    {
        float duration = Mathf.Max(0.05f, (over + 1) * checkerHopGap / Mathf.Max(0.1f, checkerHopAirSpeed));
        float rise = (checkerHopHeight + checkerHopHeightPerSlime * over) * unitsToCanvas;
        LandingX = x;
        StopAllCoroutines();
        landing?.Kill(); // still springing back from the last one
        StartCoroutine(CheckerHopArc(new Vector2(x, root.anchoredPosition.y), rise, duration));
    }

    private IEnumerator CheckerHopArc(Vector2 to, float rise, float duration)
    {
        // Round the arc at an even pace, like a point on a wheel's rim: half as high as long is a half circle
        Vector2 from = root.anchoredPosition;
        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            float angle = time / duration * Mathf.PI;
            root.anchoredPosition = Vector2.Lerp(from, to, (1f - Mathf.Cos(angle)) * 0.5f);
            body.anchoredPosition = new Vector2(0f, Mathf.Sin(angle) * rise);
            float amount = Mathf.Lerp(stretch, 1f, Mathf.Sin(angle));
            body.localScale = new Vector3(1f / amount, amount, 1f);
            Animate(Time.deltaTime);
            yield return null;
        }
        root.anchoredPosition = to;
        body.anchoredPosition = Vector2.zero;
        // Squashes down on landing, then springs back
        landing = DOTween.Sequence().SetLink(gameObject)
            .Append(body.DOScale(new Vector3(1f / squash, squash, 1f), checkerSquashTime).SetEase(Ease.OutQuad))
            .Append(body.DOScale(Vector3.one, checkerSpringBackTime).SetEase(Ease.OutBack));
        yield return Idle();
    }

    // Leaves (its line has gone off screen); a Tibbit riding it hops off
    public void Remove()
    {
        if (rider != null)
            rider.Unseat();
        Destroy(gameObject);
    }

    private IEnumerator Run(Vector2[] path)
    {
        if (movement == Movement.Dig)
            yield return Dig(path);
        else if (movement == Movement.Roll)
            yield return DropAndRoll(path);
        else if (movement == Movement.Skip && rides)
            yield return SkipAndRide(path);
        else
            for (int i = 1; i < path.Length; i++)
                yield return MoveTo(path[i]);

        // A Tibbit still riding hops off rather than going with it
        if (rider != null)
            rider.Unseat();
        Destroy(gameObject);
    }

    private IEnumerator MoveTo(Vector2 target, float maxTime = float.PositiveInfinity)
    {
        switch (movement)
        {
            case Movement.Hop: return Hops(target, hopInterval, true, maxTime, true);
            case Movement.Skip: return Hops(target, 0f, false, maxTime, false);
            case Movement.Fly: return Fly(target);
            default: return Walk(target);
        }
    }

    private void Play(Sprite[] newFrames, float fps)
    {
        if (newFrames == null || newFrames.Length == 0 || playing == newFrames) return;
        playing = newFrames;
        playingFps = fps;
        clock = 0f;
    }

    private void Animate(float deltaTime, bool inAir = false)
    {
        if (image == null) return;
        if (inAir && airSprite != null)
        {
            image.sprite = airSprite;
            return;
        }
        if (playing == null) return;
        clock += deltaTime;
        image.sprite = playing[(int)(clock * playingFps) % playing.Length];
    }

    // --- Spike balls ---

    // Falls out of the sky onto its first stop (a marker growing there as a warning), shakes the screen,
    // then rolls on to the rest
    private IEnumerator DropAndRoll(Vector2[] path)
    {
        Vector2 landing = path[Mathf.Min(1, path.Length - 1)];
        root.anchoredPosition = lastPosition = landing;
        float fallTime = Mathf.Min(dropFallTime, dropWarningTime);

        RectTransform marker = MakeMarker(out RectTransform ring);
        float wait = dropWarningTime - fallTime;
        body.gameObject.SetActive(false);
        for (float time = 0f; time < wait; time += Time.deltaTime)
        {
            SpinMarker(ring);
            yield return null;
        }

        // From above the top of the screen, speeding up as it falls
        body.gameObject.SetActive(true);
        float height = owner != null ? ((RectTransform)owner.transform).rect.height + body.rect.height : 1500f;
        for (float time = 0f; time < fallTime; time += Time.deltaTime)
        {
            float t = time / fallTime;
            body.anchoredPosition = new Vector2(0f, height * (1f - t * t));
            SpinMarker(ring);
            yield return null;
        }
        body.anchoredPosition = Vector2.zero;
        if (marker != null)
            marker.DOScale(0f, 0.15f).SetEase(Ease.InBack).SetLink(marker.gameObject)
                  .OnComplete(() => Destroy(marker.gameObject));

        if (owner != null)
            owner.Shake(landShake, landShakeTime);

        for (int i = 2; i < path.Length; i++)
        {
            rollStart = rollFrame;
            yield return Roll(path[i]);
        }
    }

    // The marker where the ball will land, facing the screen and centred on it, growing in
    private RectTransform MakeMarker(out RectTransform ring)
    {
        ring = null;
        if (markerOuter == null && markerMiddle == null) return null;
        float scale = frames != null && frames.Length > 0 && frames[0] != null ? body.rect.width / frames[0].rect.width : 5f;

        var marker = new GameObject("Marker", typeof(RectTransform)).GetComponent<RectTransform>();
        marker.SetParent(root, false);
        marker.SetAsFirstSibling(); // under the ball
        marker.anchorMin = marker.anchorMax = new Vector2(0.5f, 0.5f);
        marker.anchoredPosition = new Vector2(0f, body.rect.height * 0.5f); // around the landed ball's middle
        ring = MarkerLayer(marker, "Outer", markerOuter, scale);
        MarkerLayer(marker, "Middle", markerMiddle, scale);

        marker.localScale = Vector3.zero;
        marker.DOScale(Vector3.one, markerPopTime).SetEase(Ease.OutBack).SetLink(marker.gameObject);
        return marker;
    }

    private static RectTransform MarkerLayer(RectTransform parent, string layerName, Sprite sprite, float scale)
    {
        if (sprite == null) return null;
        var layer = new GameObject(layerName, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        layer.SetParent(parent, false);
        layer.anchorMin = layer.anchorMax = new Vector2(0.5f, 0.5f);
        layer.sizeDelta = sprite.rect.size * scale;
        var layerImage = layer.GetComponent<Image>();
        layerImage.sprite = sprite;
        layerImage.raycastTarget = false;
        return layer;
    }

    private void SpinMarker(RectTransform ring)
    {
        if (ring != null)
            ring.Rotate(0f, 0f, -markerSpin * Time.deltaTime);
    }

    // Rolls to the spot, getting going and slowing down again. It moves a frame's worth at a time, each
    // step turning it a frame (backwards going left), so it never slides along between frames.
    private IEnumerator Roll(Vector2 target)
    {
        Vector2 from = root.anchoredPosition;
        float distance = Vector2.Distance(from, target);
        if (distance < 1f) yield break;
        float duration = distance / Mathf.Max(1f, speed * unitsToCanvas) * 1.4f; // the easing's average speed
        float perFrame = Mathf.Max(1f, rollFrameDistance * unitsToCanvas);
        Vector2 direction = (target - from) / distance;
        int turnWay = target.x >= from.x ? 1 : -1;

        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            float t = time / duration;
            int steps = (int)(distance * t * t * (3f - 2f * t) / perFrame);
            root.anchoredPosition = from + direction * (steps * perFrame);
            RollFrame(steps * turnWay);
            yield return null;
        }
        root.anchoredPosition = target;
        RollFrame(Mathf.RoundToInt(distance / perFrame) * turnWay);
    }

    // Shows the frame so many steps on from where this roll began
    private void RollFrame(int step)
    {
        if (image == null || frames == null || frames.Length == 0) return;
        int count = frames.Length;
        image.sprite = frames[((rollStart + step) % count + count) % count];
        rollFrame = rollStart + step;
    }

    // Turns round (a quick flip, as in the game) when heading the other way
    private void Face(float sideways)
    {
        if (Mathf.Abs(sideways) < 0.01f || body == null) return;
        bool shouldTurn = sideways > 0f == facesLeft; // going the way it isn't drawn facing
        if (shouldTurn == turned) return;
        turned = shouldTurn;
        turn?.Kill();
        turn = body.DOLocalRotate(new Vector3(0f, turned ? 180f : 0f, 0f), 0.15f).SetLink(gameObject);
    }

    private IEnumerator Walk(Vector2 target)
    {
        float step = speed * unitsToCanvas;
        while (true)
        {
            Vector2 position = root.anchoredPosition;
            Vector2 toTarget = target - position;
            float move = step * Time.deltaTime;
            Face(toTarget.x);
            Animate(Time.deltaTime);
            if (toTarget.magnitude <= move)
            {
                root.anchoredPosition = target;
                yield break;
            }
            root.anchoredPosition = position + toTarget.normalized * move;
            yield return null;
        }
    }

    // Hop after hop toward the spot: an arc up and down, stretched leaving and nearing the ground and
    // squashed on landing (slimes, in their hop pattern), or quick light skips (Tibbits). Gives up after
    // maxTime (between hops).
    private IEnumerator Hops(Vector2 target, float interval, bool squashAndStretch, float maxTime, bool usePattern)
    {
        float stopAt = Time.time + maxTime;

        while (Time.time < stopAt)
        {
            Vector2 from = root.anchoredPosition;
            Vector2 toTarget = target - from;
            if (toTarget.magnitude < 1f) yield break;

            HopType hop = usePattern ? NextHop() : HopType.Short;
            bool isLong = hop == HopType.Long;
            float reach = Mathf.Max(1f, hopDistance * (isLong ? longHopDistance : 1f) * unitsToCanvas);
            float rise = hopHeight * (isLong ? longHopHeight : 1f) * unitsToCanvas;
            float duration = Mathf.Max(0.05f, hopDuration * (isLong ? longHopDuration : 1f));
            Vector2 to = hop == HopType.Back
                ? from - toTarget.normalized * reach // a hop back, still facing the way it's going
                : from + Vector2.ClampMagnitude(toTarget, reach);
            Face(toTarget.x);

            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                float t = time / duration;
                root.anchoredPosition = Vector2.Lerp(from, to, t);
                body.anchoredPosition = new Vector2(0f, Mathf.Sin(t * Mathf.PI) * rise);
                if (squashAndStretch)
                {
                    float amount = Mathf.Lerp(stretch, 1f, Mathf.Sin(t * Mathf.PI));
                    body.localScale = new Vector3(1f / amount, amount, 1f);
                }
                // A Tibbit's skip shows its jump sprite only off the ground, sitting as it touches down
                // between skips, as in the game
                Animate(Time.deltaTime, squashAndStretch || Mathf.Sin(t * Mathf.PI) > 0.25f);
                yield return null;
            }
            root.anchoredPosition = to;
            body.anchoredPosition = Vector2.zero;
            Animate(0f);

            if (squashAndStretch)
            {
                body.localScale = new Vector3(1f / squash, squash, 1f);
                body.DOScale(Vector3.one, Mathf.Max(0.08f, interval)).SetEase(Ease.OutBack).SetLink(gameObject);
            }
            // Short Short Long: a pause after the long one
            float wait = interval + (hopPattern == HopPattern.ShortShortLong && isLong ? longHopWait : 0f);
            for (float time = 0f; time < wait; time += Time.deltaTime)
            {
                Animate(Time.deltaTime);
                yield return null;
            }
        }
    }

    // The next hop in its pattern, chosen as the game's slimes choose theirs
    private HopType NextHop()
    {
        HopType hop;
        switch (hopPattern)
        {
            case HopPattern.ShortShortLong:
                if (shortHops >= 2)
                {
                    hop = HopType.Long;
                    shortHops = 0;
                }
                else
                {
                    hop = HopType.Short;
                    shortHops++;
                }
                break;

            case HopPattern.BackAndForward:
                if (lastHop == HopType.Back)
                {
                    // Forward after a hop back: long once enough have gone back (forced after too many)
                    if (backHops >= maxBackForward) hop = HopType.Long;
                    else if (backHops < minBackForward) hop = HopType.Short;
                    else hop = Random.value < longForwardChance ? HopType.Long : HopType.Short;
                }
                else if (backHops < minBackForward || Random.value < backChance)
                {
                    hop = HopType.Back;
                    backHops++;
                }
                else
                    hop = Random.value < longForwardChance ? HopType.Long : HopType.Short;

                if (hop == HopType.Long)
                    backHops = 0;
                break;

            default:
                hop = HopType.Short;
                break;
        }
        lastHop = hop;
        return hop;
    }

    // Up off the ground, weaving side to side across the way it's going and bobbing
    private IEnumerator Fly(Vector2 target)
    {
        float step = speed * unitsToCanvas;
        float height = flyHeight * unitsToCanvas;
        float wave = waveAmplitude * unitsToCanvas;
        float length = Mathf.Max(0.1f, waveLength * unitsToCanvas);
        float bob = bobHeight * unitsToCanvas;
        float travelled = 0f;

        while (true)
        {
            Vector2 position = root.anchoredPosition;
            Vector2 toTarget = target - position;
            float move = step * Time.deltaTime;
            Vector2 across = new Vector2(-toTarget.y, toTarget.x).normalized;
            travelled += move;

            float weave = Mathf.Sin(travelled / length * Mathf.PI * 2f) * wave;
            float bobbing = Mathf.Sin(Time.time * bobsPerSecond * Mathf.PI * 2f) * bob;
            body.anchoredPosition = across * weave + Vector2.up * (height + bobbing);
            Face(toTarget.x);
            Animate(Time.deltaTime);

            if (toTarget.magnitude <= move)
            {
                root.anchoredPosition = target;
                yield break;
            }
            root.anchoredPosition = position + toTarget.normalized * move;
            yield return null;
        }
    }

    // --- Mollies ---

    // Travels underground as a moving mound (its dig frames), and at each stop on screen peeks up out of
    // it, looks around and ducks back down, as in the game
    private IEnumerator Dig(Vector2[] path)
    {
        for (int i = 1; i < path.Length; i++)
        {
            Play(digFrames, digFramesPerSecond);
            yield return Walk(path[i]);
            if (i == path.Length - 1) break; // the last stop is off screen: it digs away

            yield return PlayOnce(peekFrames, peekFramesPerSecond, false);
            playing = null;
            Play(frames, framesPerSecond);
            up = true;
            float idle = Random.Range(idleTime.x, idleTime.y);
            for (float time = 0f; time < idle; time += Time.deltaTime)
            {
                Animate(Time.deltaTime);
                yield return null;
            }

            // Ducking: a Tibbit on her (or jumping on) gets off first
            up = false;
            for (float wait = 0f; rider != null && wait < 0.5f; wait += Time.deltaTime)
            {
                Animate(Time.deltaTime);
                yield return null;
            }
            yield return PlayOnce(peekFrames, peekFramesPerSecond, true);
            playing = null;
        }
    }

    private IEnumerator PlayOnce(Sprite[] once, float fps, bool backwards)
    {
        if (once == null || once.Length == 0) yield break;
        playing = null; // stop the looping frames
        float length = once.Length / Mathf.Max(0.1f, fps);
        for (float time = 0f; time < length; time += Time.deltaTime)
        {
            int index = Mathf.Min(once.Length - 1, (int)(time * fps));
            image.sprite = once[backwards ? once.Length - 1 - index : index];
            yield return null;
        }
    }

    // --- Tibbits ---

    private Vector2 Goal => tibbitPath[Mathf.Min(tibbitNext, tibbitPath.Length - 1)];

    // Where it is in the spawner's space, riding or not
    private Vector2 Here => carrier != null && owner != null
        ? (Vector2)owner.transform.InverseTransformPoint(root.position)
        : root.anchoredPosition;

    private bool ReachedGoal(Vector2 here) => Mathf.Abs(here.x - Goal.x) < 40f && Mathf.Abs(here.y - Goal.y) < 80f;

    // Heading roughly toward where the Tibbit wants to go from here
    private bool GoingMyWay(MenuCritter critter, Vector2 here)
    {
        if (critter.StandsStill) return true;
        Vector2 toGoal = Goal - here;
        if (critter.velocity.sqrMagnitude < 25f || toGoal.sqrMagnitude < 1f) return false;
        return Vector2.Dot(critter.velocity.normalized, toGoal.normalized) >= sameWay;
    }

    // Skips toward each stop, riding any critter heading its way when one comes within reach
    private IEnumerator SkipAndRide(Vector2[] path)
    {
        tibbitPath = path;
        tibbitNext = 1;
        float noRideUntil = 0f;

        while (tibbitNext < path.Length)
        {
            MenuCritter host = Time.time >= noRideUntil ? FindHost(null) : null;
            if (host != null)
            {
                yield return Ride(host);
                noRideUntil = Time.time + rideCooldown;
                continue;
            }

            yield return MoveTo(Goal, searchInterval);
            if (ReachedGoal(root.anchoredPosition))
                tibbitNext++;
        }
    }

    // The biggest critter in reach that's on screen, free and heading its way (the nearest of equal size)
    private MenuCritter FindHost(MenuCritter except)
    {
        if (owner == null || tibbitNext >= tibbitPath.Length) return null;
        Vector2 here = Here;
        float reach = jumpRange * unitsToCanvas;
        MenuCritter best = null;
        float bestSize = 0f, bestDistance = float.MaxValue;
        foreach (MenuCritter critter in all)
        {
            if (critter == this || critter == except || !critter.CanCarry || ridden.Contains(critter)) continue;
            if (!owner.OnScreen(critter.root.anchoredPosition, 100f) || !GoingMyWay(critter, here)) continue;
            float distance = Vector2.Distance(here, critter.Seat());
            if (distance > reach) continue;
            float size = critter.Size;
            if (size > bestSize + 1f || (Mathf.Abs(size - bestSize) <= 1f && distance < bestDistance))
            {
                best = critter;
                bestSize = size;
                bestDistance = distance;
            }
        }
        return best;
    }

    // Jumps on and rides until its ride turns away, is about to leave the screen, it's got where it wanted
    // or it's ridden long enough; then across to another ride heading its way, or down to the ground
    private IEnumerator Ride(MenuCritter host)
    {
        while (host != null)
        {
            host.rider = this;
            ridden.Add(host);
            yield return JumpTo(() => host != null ? host.Seat() : Here);
            if (host == null || !host.CanSeat(this))
            {
                if (host != null && host.rider == this)
                    host.rider = null; // she ducked (or it was taken) while it was in the air
                break;
            }

            // On its back: carried along, bobbing with it
            carrier = host;
            root.SetParent(host.body, false);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0f);
            root.anchoredPosition = host.SeatOnBody();
            body.anchoredPosition = Vector2.zero;
            Play(frames, framesPerSecond);

            // On a Molly it stays until she ducks; on anything else no longer than its ride time
            float until = host.StandsStill ? float.PositiveInfinity : Time.time + Random.Range(rideTime.x, rideTime.y);
            float wrongWay = 0f;
            while (carrier == host && host != null && Time.time < until)
            {
                Animate(Time.deltaTime);
                Vector2 here = Here;
                if (host.StandsStill && !host.up) break;
                if (!owner.OnScreen(host.root.anchoredPosition, 120f)) break;
                if (ReachedGoal(here))
                {
                    tibbitNext++;
                    if (tibbitNext >= tibbitPath.Length || !GoingMyWay(host, here)) break;
                }
                wrongWay = GoingMyWay(host, here) ? 0f : wrongWay + Time.deltaTime;
                if (wrongWay > wrongWayGrace) break;
                yield return null;
            }

            MenuCritter last = host;
            if (carrier == host)
                Unseat();
            if (host != null && host.rider == this)
                host.rider = null;
            host = FindHost(last);
        }

        // Down to the ground near where it is
        Vector2 landing = owner.OnGroundNear(Here + Vector2.right * Random.Range(-80f, 80f));
        yield return JumpTo(() => landing);
        Play(frames, framesPerSecond);
    }

    private bool CanSeat(MenuCritter tibbit) => rider == tibbit && root != null && (movement != Movement.Dig || up);

    // Where a rider sits, in the spawner's space: on top of the body
    private Vector2 Seat() => root.anchoredPosition + body.anchoredPosition + Vector2.up * body.rect.height * 0.8f;

    // The same spot in the body's own space (its pivot is at its feet)
    private Vector2 SeatOnBody() => new(0f, body.rect.height * 0.8f);

    // Back into the spawner's space where it is, off whatever it was riding (which may be leaving)
    private void Unseat()
    {
        if (carrier == null || owner == null) return;
        if (carrier.rider == this)
            carrier.rider = null;
        carrier = null;
        Vector3 where = root.position;
        root.SetParent(owner.transform, false);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one;
        root.position = where;
    }

    // An arc to a spot that may be moving (a critter's back), at its jump speed, showing its jump sprite
    private IEnumerator JumpTo(Func<Vector2> target)
    {
        Vector2 from = root.anchoredPosition;
        float distance = Vector2.Distance(from, target());
        float duration = Mathf.Clamp(distance / Mathf.Max(0.1f, jumpSpeed * unitsToCanvas), jumpTime.x, jumpTime.y);
        float rise = jumpHeight * unitsToCanvas;

        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            float t = time / duration;
            Vector2 to = target();
            Face(to.x - from.x);
            root.anchoredPosition = Vector2.Lerp(from, to, t);
            body.anchoredPosition = new Vector2(0f, Mathf.Sin(t * Mathf.PI) * rise);
            Animate(Time.deltaTime, true);
            yield return null;
        }
        root.anchoredPosition = target();
        body.anchoredPosition = Vector2.zero;

        // Landed: back to sitting straight away
        Play(frames, framesPerSecond);
        Animate(0f);
    }

#if UNITY_EDITOR
    // For Tools > Set Up Main Menu Critters: the enemy's numbers, read from its data
    [Serializable]
    public struct Settings
    {
        public Movement movement;
        public Sprite[] frames, digFrames, peekFrames;
        public float framesPerSecond, digFramesPerSecond, peekFramesPerSecond;
        public Sprite airSprite;
        public bool facesLeft;
        public float speed, hopHeight, hopDuration, hopInterval, hopDistance, squash, stretch;
        public HopPattern hopPattern;
        public float longHopDistance, longHopHeight, longHopDuration, longHopWait, backChance, longForwardChance;
        public int minBackForward, maxBackForward;
        public float flyHeight, waveAmplitude, waveLength, bobHeight, bobsPerSecond;
        public Vector2 idleTime;
        public bool rides;
        public Vector2 rideTime, jumpTime;
        public float jumpRange, jumpHeight, jumpSpeed, searchInterval;
        public int groupSize, maxGroupSize;
        public float groupSpacing, weight;
        public Sprite markerOuter, markerMiddle;
        public float dropWarningTime, markerPopTime, markerSpin, dropFallTime;
        public Sprite[] altFrames;
        public float checkerHopHeight, checkerHopHeightPerSlime, checkerHopGap, checkerHopAirSpeed;
    }

    public void Apply(Settings settings, RectTransform bodyRect, Image bodyImage)
    {
        body = bodyRect;
        image = bodyImage;
        movement = settings.movement;
        frames = settings.frames;
        framesPerSecond = settings.framesPerSecond;
        airSprite = settings.airSprite;
        facesLeft = settings.facesLeft;
        speed = settings.speed;
        hopHeight = settings.hopHeight;
        hopDuration = settings.hopDuration;
        hopInterval = settings.hopInterval;
        hopDistance = settings.hopDistance;
        squash = settings.squash;
        stretch = settings.stretch;
        flyHeight = settings.flyHeight;
        waveAmplitude = settings.waveAmplitude;
        waveLength = settings.waveLength;
        bobHeight = settings.bobHeight;
        bobsPerSecond = settings.bobsPerSecond;
        groupSize = settings.groupSize;
        maxGroupSize = settings.maxGroupSize;
        groupSpacing = settings.groupSpacing;
        weight = settings.weight;
        FillMissing(settings);
    }

    // Settings added since the prefab was made, filled in from the enemy without touching anything set.
    // Returns whether anything changed.
    public bool FillMissing(Settings settings)
    {
        bool changed = false;
        if (movement == Movement.Hop && settings.hopPattern != HopPattern.Regular && hopPattern == HopPattern.Regular)
        {
            hopPattern = settings.hopPattern;
            longHopDistance = settings.longHopDistance;
            longHopHeight = settings.longHopHeight;
            longHopDuration = settings.longHopDuration;
            longHopWait = settings.longHopWait;
            backChance = settings.backChance;
            longForwardChance = settings.longForwardChance;
            minBackForward = settings.minBackForward;
            maxBackForward = settings.maxBackForward;
            changed = true;
        }
        if (movement == Movement.Dig && (peekFrames == null || peekFrames.Length == 0))
        {
            digFrames = settings.digFrames;
            digFramesPerSecond = settings.digFramesPerSecond;
            peekFrames = settings.peekFrames;
            peekFramesPerSecond = settings.peekFramesPerSecond;
            idleTime = settings.idleTime;
            changed = true;
        }
        if (movement == Movement.Roll && markerOuter == null && markerMiddle == null)
        {
            markerOuter = settings.markerOuter;
            markerMiddle = settings.markerMiddle;
            dropWarningTime = settings.dropWarningTime;
            markerPopTime = settings.markerPopTime;
            markerSpin = settings.markerSpin;
            dropFallTime = settings.dropFallTime;
            changed = true;
        }
        if (movement == Movement.CheckerHop && (altFrames == null || altFrames.Length == 0))
        {
            altFrames = settings.altFrames;
            checkerHopHeight = settings.checkerHopHeight;
            checkerHopHeightPerSlime = settings.checkerHopHeightPerSlime;
            checkerHopGap = settings.checkerHopGap;
            checkerHopAirSpeed = settings.checkerHopAirSpeed;
            changed = true;
        }
        if (settings.rides && jumpRange <= 0f)
        {
            rides = true;
            rideTime = settings.rideTime;
            jumpRange = settings.jumpRange;
            jumpHeight = settings.jumpHeight;
            jumpSpeed = settings.jumpSpeed;
            jumpTime = settings.jumpTime;
            searchInterval = settings.searchInterval;
            changed = true;
        }
        return changed;
    }
#endif
}
