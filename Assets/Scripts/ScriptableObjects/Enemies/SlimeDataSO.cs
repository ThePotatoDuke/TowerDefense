using UnityEngine;

[CreateAssetMenu(fileName = "SlimeData", menuName = "Enemies/SlimeData")]
public class SlimeDataSO : EnemyDataSO
{
    [Header("Bounce Settings")]
    [Tooltip("Height of the bounce for normal hops.")]
    public float bounceHeight = 1f;

    [Tooltip("Duration of the bounce up and down.")]
    public float bounceDuration = 0.3f;

    [Tooltip("Time to wait between consecutive bounces.")]
    public float bounceInterval = 0.1f;

    [Header("Hop Distances")]
    [Tooltip("Base distance of a short hop.")]
    public float baseHopDistance = 1f;

    [Tooltip("Multiplier applied to base hop distance for long forward hops.")]
    public float longHopDistanceMultiplier = 2f;

    [Header("Hop Pattern")]
    [Tooltip("Pattern for hop sequence.")]
    public HopPattern hopPattern = HopPattern.Regular;

    [Tooltip("Maximum consecutive back+forward hops before forcing a long hop.")]
    public int maxBackForward = 3;

    [Tooltip("Minimum consecutive back+forward hops before allowing a long hop.")]
    public int minBackForward = 1;

    [Tooltip("Wait time after a long hop in ShortShortLong pattern.")]
    public float longHopWaitTime = 0.5f;

    [Tooltip("Chance of performing a backward hop after front hop (0 = never, 1 = always).")]
    [Range(0f, 1f)]
    public float backChance = 0.5f;

    [Tooltip("Chance of performing a forward long hop after back hop (0 = never, 1 = always).")]
    [Range(0f, 1f)]
    public float longForwardChance = 0.5f;

    [Header("Long Hop Tweaks")]
    [Tooltip("Height multiplier for long forward hops.")]
    public float longHopHeightMultiplier = 1.5f;

    [Tooltip("Duration multiplier for long forward hops.")]
    public float longHopDurationMultiplier = 1.3f;

    [Header("Stretch/Squash")]
    [Tooltip("Vertical stretch when taking off.")]
    public float takeoffStretch = 1.2f;

    [Tooltip("Vertical stretch when falling.")]
    public float fallStretch = 1.3f;

    [Tooltip("Vertical squash applied on landing.")]
    public float squash = 0.8f;

    public enum HopPattern { Regular, ShortShortLong, BackAndForward }
}
