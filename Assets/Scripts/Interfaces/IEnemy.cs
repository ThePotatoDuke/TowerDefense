using UnityEngine;

public interface IEnemy
{
    float ContactDamage { get; }
    void Move();
    float Progress { get; }
}
