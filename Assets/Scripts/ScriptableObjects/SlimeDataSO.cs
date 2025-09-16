using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/Slime Data")]
public class SlimeDataSO : EnemyDataSO
{
    [Header("Slime Settings")]
    public float bounceHeight = 1f;
    public float bounceDuration = 0.5f;
    public float bounceInterval = 0.5f;

    [Header("Movement")]
    public float moveDistance = 2f;  // new field for how far slime moves horizontally
}
