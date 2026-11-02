using UnityEngine;
using UnityEngine.Serialization;

// Checker Slimes: come in an even group of black and white slimes in a checker line, and get along the path
// by leapfrog, the one at the back jumping over the rest to land in front, again and again, so the line
// turns over like a wheel rolling. All of them behave the same; the colours are only the pattern.
[CreateAssetMenu(fileName = "CheckerSlimeData", menuName = "Enemies/Checker Slime Data")]
public class CheckerSlimeDataSO : EnemyDataSO
{
    [Header("Colours")]
    [Tooltip("The animations for each colour (overrides of the slime's base controller).")]
    public RuntimeAnimatorController blackAnimator;
    public RuntimeAnimatorController whiteAnimator;

    [Header("Group")]
    [Tooltip("Fewest and most in a group; always an even number, so the checker holds as they turn over.")]
    public int minGroupSize = 2;
    public int maxGroupSize = 6;
    [Tooltip("World units between neighbours in the line.")]
    public float spacing = 1.2f;
    [Tooltip("Seconds after spawning before the first jump (while they pop in).")]
    public float startDelay = 0.5f;

    [Header("Checker Hop")]
    [Tooltip("Height of a jump over no one (a lone slime's hop), in world units.")]
    [FormerlySerializedAs("jumpHeight")] public float checkerHopHeight = 0.6f;
    [Tooltip("Extra height for each slime it jumps over.")]
    [FormerlySerializedAs("heightPerSlime")] public float checkerHopHeightPerSlime = 0.6f;
    [Tooltip("Seconds between one taking off and the next, without waiting for landings. Sets the group's " +
             "speed: one space every this many seconds.")]
    [FormerlySerializedAs("takeoffGap")] public float checkerHopGap = 0.6f;
    [Tooltip("How many times faster than the line a jumper flies (2 = like the top of a rolling wheel). " +
             "Higher means quicker jumps and fewer in the air at once.")]
    [FormerlySerializedAs("airSpeed")] public float checkerHopAirSpeed = 3f;

    [Header("Stretch/Squash")]
    [Tooltip("Vertical stretch when taking off.")]
    public float takeoffStretch = 1.25f;
    [Tooltip("Vertical stretch when falling.")]
    public float fallStretch = 1.2f;
    [Tooltip("Vertical squash applied on landing.")]
    public float squash = 0.75f;
    [Tooltip("Seconds it takes to squash down on landing, then to spring back (with a little wobble).")]
    public float squashTime = 0.1f;
    public float springBackTime = 0.32f;

    // How many in the next group: an even number between the two
    public int RollGroupSize()
    {
        int least = Mathf.Max(2, minGroupSize + minGroupSize % 2);
        int most = Mathf.Max(least, maxGroupSize - maxGroupSize % 2);
        return Random.Range(least / 2, most / 2 + 1) * 2;
    }

    // Seconds in the air jumping over this many others: the distance at the jumpers' speed
    public float HopDuration(int over) => (over + 1) * checkerHopGap / Mathf.Max(0.1f, checkerHopAirSpeed);

    public float AverageGroupSize => (Mathf.Max(2, minGroupSize) + Mathf.Max(2, maxGroupSize)) * 0.5f;
}
