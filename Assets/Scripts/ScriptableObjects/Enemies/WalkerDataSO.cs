using UnityEngine;

[CreateAssetMenu(fileName = "WalkerData", menuName = "Enemies/WalkerData")]
public class WalkerDataSO : EnemyDataSO
{
    [Header("Movement")]
    [Tooltip("Speed at which the walker moves.")]
    public float moveSpeed = 2f;
}
