using UnityEngine;

[CreateAssetMenu(fileName = "MollyData", menuName = "Enemies/Molly Data")]
public class MollyDataSO : EnemyDataSO
{
    [Header("Digging (underground: can't be hit)")]
    [Tooltip("Speed along the path while digging.")]
    public float digSpeed = 2.5f;
    [Tooltip("Seconds spent digging before popping up; picked at random between the two.")]
    public float minDigTime = 2f;
    public float maxDigTime = 3.5f;

    [Header("Above ground (can be hit)")]
    [Tooltip("Seconds spent idling above ground before ducking back down; picked at random between the two.")]
    public float minIdleTime = 1.5f;
    public float maxIdleTime = 2.5f;

    [Header("Hiding from rollers")]
    [Tooltip("When a roller gets this close (to its surface), Molly ducks underground and stays there.")]
    public float rollerDangerDistance = 2.5f;
    [Tooltip("Seconds to wait after the roller has gone before coming back up.")]
    public float resurfaceDelay = 0.4f;

    [Header("Animation (frames face left)")]
    public Sprite[] digFrames;
    [Tooltip("Coming up out of the ground, first frame underground. Played backwards to duck back down.")]
    public Sprite[] peekFrames;
    public Sprite[] idleFrames;
    public float digFramesPerSecond = 10f;
    public float peekFramesPerSecond = 12f;
    public float idleFramesPerSecond = 4f;
}
