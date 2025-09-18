using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/SpikeBall Data")]
public class SpikeBallDataSO : EnemyDataSO
{
    [Header("Spring Settings")]
    public float springK = 50f;
    public float damping = 18f;

    [Header("Movement")]
    public float ballRadius = 0.5f;
    public float targetProgress;
}

