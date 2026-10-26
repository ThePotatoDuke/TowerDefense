using UnityEngine;

[CreateAssetMenu(fileName = "FrillsData", menuName = "Enemies/Frills Data")]
public class FrillsDataSO : EnemyDataSO
{
    [Header("Movement (sky-high: only guns that aim up reach it)")]
    [Tooltip("Speed along the path at the start; it changes evenly to the end speed by the castle. " +
             "Same for both keeps it steady, start high to slow down, start low to speed up.")]
    public float startSpeed = 1.5f;
    public float endSpeed = 1.5f;
    [Tooltip("Height it flies at above the path.")]
    public float flyHeight = 2.4f;
    [Tooltip("Seconds to rise from the ground to flying height after spawning.")]
    public float takeOffTime = 0.8f;
    [Tooltip("Gentle floating up and down.")]
    public float bobHeight = 0.1f;
    public float bobsPerSecond = 0.8f;

    [Header("Shooting (at the player, the landing spot marked on the ground)")]
    [Tooltip("Seconds between shots; picked at random between the two.")]
    public float minShootInterval = 3f;
    public float maxShootInterval = 5f;
    [Tooltip("Only shoots when the player is at most this far away (across the ground).")]
    public float shootRange = 10f;
    [Tooltip("Seconds from firing to the shot landing, which is how long the marker warns for.")]
    public float shotTravelTime = 1.1f;
    public float shotDamage = 1f;
    [Tooltip("How close to where the shot lands the player has to be to get hit.")]
    public float shotHitRadius = 0.6f;
    public Sprite[] shotFrames;
    public float shotFramesPerSecond = 12f;

    [Header("Animation")]
    public Sprite[] idleFrames;
    public float idleFramesPerSecond = 8f;
}
