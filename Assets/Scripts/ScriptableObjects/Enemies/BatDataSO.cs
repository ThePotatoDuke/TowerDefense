using UnityEngine;

[CreateAssetMenu(fileName = "BatData", menuName = "Enemies/BatData")]
public class BatDataSO : EnemyDataSO
{
    [Header("Movement")]
    [Tooltip("Speed along the path.")]
    public float moveSpeed = 2.5f;
    [Tooltip("Lowest flying height above the path.")]
    public float hoverHeight = 0.4f;
    [Tooltip("Seconds to rise from the ground to hover height after spawning.")]
    public float takeOffTime = 0.6f;

    [Header("Weaving (side to side)")]
    [Tooltip("How far the bat swings to each side of the path.")]
    public float waveAmplitude = 1.2f;
    [Tooltip("Distance along the path for one full side-to-side swing.")]
    public float waveLength = 4f;

    [Header("Climbing (up and down)")]
    [Tooltip("How far above hover height the bat climbs. 0 keeps it low.")]
    public float climbHeight = 0f;
    [Tooltip("Distance along the path for one full climb and dive.")]
    public float climbLength = 6f;
    [Tooltip("Above this height only guns that can aim up target the bat.")]
    public float aimUpHeight = 1f;

    [Header("Flapping")]
    [Tooltip("Wing beats per second; match the flap animation.")]
    public float flapRate = 2.4f;
    [Tooltip("How far the body bobs up and down with each wing beat.")]
    public float bobHeight = 0.06f;

    [Header("Visuals")]
    [Tooltip("Color of the motion trail and pixels left behind.")]
    public Color trailColor = new Color(0.8f, 0.8f, 0.86f, 1f);
}
