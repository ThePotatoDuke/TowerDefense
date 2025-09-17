using System;
using UnityEngine;

public abstract class EnemyBase : MonoBehaviour, IHasHealth, IEnemy
{
    // --- Health & damage ---
    protected float currentHealth;

    public abstract EnemyDataSO Data { get; }

    public float CurrentHealth => currentHealth;
    public float MaxHealth => Data.maxHealth;
    public float ContactDamage => Data.contactDamage;

    public event Action<float> OnHealthChanged;
    public event Action OnDied;

    protected virtual void Awake()
    {
        currentHealth = Data.maxHealth;
    }


    // --- Health methods ---
    public virtual void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);
        OnHealthChanged?.Invoke(currentHealth / MaxHealth);

        if (currentHealth <= 0f)
            Die();
    }

    protected virtual void Die()
    {
        OnDied?.Invoke();
        Destroy(gameObject, 1f);
    }

    // --- Movement placeholder ---
    public virtual void Move()
    {
        throw new NotImplementedException();
    }
}
