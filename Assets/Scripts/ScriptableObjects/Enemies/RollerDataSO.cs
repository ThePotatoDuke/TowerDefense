using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/Roller Data")]
public class RollerDataSO : EnemyDataSO
{
    [Header("Movement")]
    public float moveSpeed = 2.5f;
    [Tooltip("Radius of the cylinder. Sets how fast it spins and how close two rollers get before bouncing apart.")]
    public float radius = 0.4f;

    [Header("Dodging")]
    [Tooltip("How high enemies in the roller's way hop to let it pass underneath.")]
    public float dodgeJumpHeight = 1.2f;
    [Tooltip("Duration (seconds) of that hop. Enemies take off early enough to be at the top when the roller arrives.")]
    public float dodgeJumpDuration = 0.6f;

    [Header("Lifetime")]
    [Tooltip("Number of cleared waves after which the roller destroys itself.")]
    public int wavesToSurvive = 3;
}
