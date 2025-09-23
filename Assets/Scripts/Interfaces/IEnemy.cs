using UnityEngine;

public interface IEnemy
{
    float ContactDamage { get; }
    void Move();
    float DistancePercentage { get; }
}
