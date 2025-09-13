using System;
using UnityEngine;

public interface IHasHealth
{
    float CurrentHealth { get; }
    float MaxHealth { get; }
    public event Action<float> OnHealthChanged;
    void TakeDamage(float amount);
}
