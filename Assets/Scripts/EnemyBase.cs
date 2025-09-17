using UnityEngine;
using System;
using DG.Tweening;

public abstract class EnemyBase : MonoBehaviour, IHasHealth, IEnemy
{
    // --- Health & damage ---
    protected float currentHealth;
    protected SpriteRenderer spriteRenderer;

    public abstract EnemyDataSO Data { get; }

    public float CurrentHealth => currentHealth;
    public float MaxHealth => Data.maxHealth;
    public float ContactDamage => Data.contactDamage;

    public event Action<float> OnHealthChanged;
    public event Action OnDied;


    [SerializeField] private Transform spriteTransform;
    [SerializeField] private LookAtCamera lookAtCamera; // Reference to LookAtCamera


    private Vector3 lastPos;
    // private int facing = 1; // +1 = right, -1 = left
    private enum Facing
    {
        Right,
        Left
    }

    private Facing currentFacing;


    protected virtual void Awake()
    {
        currentFacing = Facing.Right;

        currentHealth = Data.maxHealth;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.color = Data.color;

        lastPos = transform.position;
    }

    protected virtual void Update()
    {
        HandleFlip();
    }

    private void HandleFlip()
    {
        Vector3 movement = transform.position - lastPos;

        if (movement.sqrMagnitude > 0.00001f)
        {
            Facing newFacing = movement.x >= 0 ? Facing.Right : Facing.Left;

            if (newFacing != currentFacing)
            {
                currentFacing = newFacing;
                Flip(currentFacing);
            }
        }

        lastPos = transform.position;
    }

    private void Flip(Facing dir)
    {

        float yRot = (dir == Facing.Right) ? 0f : 180f;

        spriteTransform.DOLocalRotate(new Vector3(0, yRot, 0), Constants.flipDuration, RotateMode.Fast);

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
