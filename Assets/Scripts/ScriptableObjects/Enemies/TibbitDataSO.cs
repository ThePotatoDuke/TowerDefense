using UnityEngine;

[CreateAssetMenu(fileName = "TibbitData", menuName = "Enemies/Tibbit Data")]
public class TibbitDataSO : EnemyDataSO
{
    [Header("Riding (can't be hit)")]
    [Tooltip("Seconds it rides one enemy before jumping on to the next one ahead; picked at random between the two.")]
    public float minRideTime = 2.5f;
    public float maxRideTime = 4f;
    [Tooltip("Farthest away an enemy can be for it to jump onto.")]
    public float jumpRange = 6f;
    [Tooltip("Seconds between looks for an enemy to jump onto, when there was none in reach.")]
    public float searchInterval = 0.4f;
    [Tooltip("Jump off and run for the castle alone when nobody ahead has been in reach for a while.")]
    public bool runsWhenStuck;
    [Tooltip("How long nobody ahead has to be in reach (after its ride time is up) before it runs for it.")]
    public float stuckTime = 1.5f;

    [Header("When its ride dies")]
    [Tooltip("Falls off and dies with it. Otherwise it falls off dazed, then carries on alone.")]
    public bool diesWithHost;
    [Tooltip("Seconds it sits dazed on the ground after falling off (can be hit).")]
    public float dazeTime = 1.2f;

    [Header("Jumping")]
    [Tooltip("Height of the arc above the straight line to where it lands.")]
    public float jumpHeight = 1.2f;
    [Tooltip("How fast it covers ground in the air. Jumps take between the shortest and longest time below.")]
    public float jumpSpeed = 7f;
    public float minJumpTime = 0.35f;
    public float maxJumpTime = 0.8f;

    [Header("On the ground (can be hit)")]
    [Tooltip("Speed along the path when it has nobody to ride.")]
    public float groundSpeed = 1.5f;
    [Tooltip("The little hops it moves in on the ground.")]
    public float groundHopHeight = 0.15f;
    public float groundHopsPerSecond = 3f;
    [Tooltip("When it gets this close to the castle (along the path), it leaps at the castle instead of riding on.")]
    public float castleLeapDistance = 4f;

    [Header("Animation (frames face left)")]
    public Sprite[] idleFrames;
    public float idleFramesPerSecond = 4f;
    public Sprite hopSprite;
}
