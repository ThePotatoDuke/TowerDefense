using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/Walker Data")]
public class WalkerDataSO : EnemyDataSO
{
    [Header("Walker Settings")]
    public float walkSpeed = 2f;
}
