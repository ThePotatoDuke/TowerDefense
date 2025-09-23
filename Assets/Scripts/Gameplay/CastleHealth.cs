using System;
using UnityEngine;

public class CastleHealth : MonoBehaviour, IHasHealth
{
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    public event Action<float> OnHealthChanged;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void OnEnable()
    {
        EnemyBase.OnEnemyReachedEnd += TakeDamageFromEnemy;
    }

    private void OnDisable()
    {
        EnemyBase.OnEnemyReachedEnd -= TakeDamageFromEnemy;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);

        if (currentHealth <= 0)
            Die();
    }

    private void TakeDamageFromEnemy(EnemyBase enemy)
    {
        TakeDamage(enemy.ContactDamage);
    }

    private void Die()
    {
        GameEvents.CastleDied();
        GameStateManager.SetState(GameState.CastleDestroyed);
    }

}
