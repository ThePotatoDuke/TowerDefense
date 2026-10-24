using UnityEngine;

[CreateAssetMenu(fileName = "BomberrData", menuName = "Enemies/Bomberr Data")]
public class BomberrDataSO : EnemyDataSO
{
    [Header("Movement")]
    [Tooltip("Speed along the path while carrying the bomb.")]
    public float moveSpeed = 1.6f;

    [Header("Carriers")]
    [Tooltip("How many little bomberrs carry the bomb.")]
    public int carrierCount = 4;
    [Tooltip("How far the carriers stand from the middle of the bomb.")]
    public float carrierSpacing = 0.45f;
    [Tooltip("How far the carriers are shifted toward the camera, so the bomb doesn't hide them.")]
    public float carrierShiftTowardCamera = 0.35f;
    [Tooltip("Carrying animation frames (facing left).")]
    public Sprite[] carryFrames;
    [Tooltip("Running animation frames (facing left), used when they drop the bomb and flee.")]
    public Sprite[] runFrames;
    public float carryFramesPerSecond = 10f;
    public float runFramesPerSecond = 14f;
    [Tooltip("Material for the carrier sprites (the default sprite material).")]
    public Material spriteMaterial;

    [Header("Throw (when defeated or at the castle)")]
    public float throwHeight = 2.5f;
    [Tooltip("Furthest the bomb lands from where it was dropped.")]
    public float throwDistance = 1.2f;
    public float throwDuration = 0.9f;
    [Tooltip("How fast the carriers run away from the bomb.")]
    public float fleeSpeed = 3f;
    [Tooltip("Seconds the carriers run before they've faded out.")]
    public float fleeDuration = 1f;

    [Header("Fuse")]
    [Tooltip("Seconds from landing to the explosion.")]
    public float fuseTime = 2.5f;
    [Tooltip("Glow color of the bomb's pulses; brighter than 1 glows with bloom.")]
    [ColorUsage(false, true)] public Color glowColor = new Color(2.5f, 0.35f, 0.15f, 1f);
    [Tooltip("Seconds between pulses when the fuse is lit; they speed up toward the end interval.")]
    public float startPulseInterval = 0.6f;
    public float endPulseInterval = 0.08f;
    [Tooltip("Color of the ring on the ground that shows how far the blast will reach.")]
    [ColorUsage(false, true)] public Color warningColor = new Color(2f, 0.3f, 0.2f, 1f);

    [Header("Explosion")]
    public float explosionRadius = 3.5f;
    [Tooltip("Damage to every enemy in the blast, including other bomberrs and spike balls.")]
    public float enemyDamage = 60f;
    public float playerDamage = 3f;
    public float castleDamage = 10f;
}
